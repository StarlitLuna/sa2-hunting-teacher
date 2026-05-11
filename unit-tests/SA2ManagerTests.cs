using sa2_hunting_teacher;
using sa2_hunting_teacher.Knuckles;
using sa2_hunting_teacher.Rouge;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace unit_tests;

[Collection(StaticStateCollection.Name)]
public class SA2ManagerTests : IDisposable {
	private readonly SA2Manager sa2;
	private readonly MemoryMappedFile mmf;
	private readonly MemoryMappedViewAccessor accessor;
	private readonly bool initialCanRun;
	private readonly MemoryMappedFile? initialMapper;
	private readonly TextBox? initialLogBox;
	private readonly bool appDataDirExisted;
	private readonly bool settingsFileExisted;
	private readonly byte[]? backupContent;

	public SA2ManagerTests() {
		this.initialCanRun = Reflect.GetStatic<bool>(typeof(SA2Manager), "CanRun");
		this.initialMapper = Reflect.GetStatic<MemoryMappedFile?>(typeof(SA2Manager), "MemoryMapper");
		this.initialLogBox = Reflect.GetStatic<TextBox?>(typeof(HuntingTeacherForm), "CurrentLogBox");

		this.appDataDirExisted = Directory.Exists(Settings.AppDataPath);
		this.settingsFileExisted = File.Exists(Settings.SettingsPath);
		if (this.settingsFileExisted) {
			this.backupContent = File.ReadAllBytes(Settings.SettingsPath);
		}

		Reflect.SetStatic(typeof(SA2Manager), "CanRun", true);
		Reflect.SetStatic(typeof(SA2Manager), "MemoryMapper", null);

		this.mmf = MemoryMappedFile.CreateNew(null, Marshal.SizeOf<HunterTeacherData>());
		this.accessor = this.mmf.CreateViewAccessor();

		this.sa2 = (SA2Manager)RuntimeHelpers.GetUninitializedObject(typeof(SA2Manager));
		Reflect.SetField(this.sa2, "sharedMemory", this.accessor);
		Reflect.SetField(this.sa2, "repetitionsInPlace", false);
	}

	public void Dispose() {
		MemoryMappedFile? leaked = Reflect.GetStatic<MemoryMappedFile?>(typeof(SA2Manager), "MemoryMapper");
		if (leaked != null && !ReferenceEquals(leaked, this.initialMapper)) {
			try { leaked.Dispose(); } catch { /* test may have already disposed it */ }
		}
		Reflect.SetStatic(typeof(SA2Manager), "MemoryMapper", this.initialMapper);
		Reflect.SetStatic(typeof(SA2Manager), "CanRun", this.initialCanRun);
		Reflect.SetStatic(typeof(HuntingTeacherForm), "CurrentLogBox", this.initialLogBox);

		this.accessor.Dispose();
		this.mmf.Dispose();

		if (!Directory.Exists(Settings.AppDataPath)) {
			Directory.CreateDirectory(Settings.AppDataPath);
		}

		if (this.settingsFileExisted) {
			File.WriteAllBytes(Settings.SettingsPath, this.backupContent!);
		} else if (File.Exists(Settings.SettingsPath)) {
			File.Delete(Settings.SettingsPath);
		}

		if (!this.appDataDirExisted) {
			try {
				Directory.Delete(Settings.AppDataPath, recursive: false);
			} catch {
			}
		}

		GC.SuppressFinalize(this);
	}

	#region ApplyDataDefaults

	[Theory]
	[InlineData(LevelId.WildCanyon)]
	[InlineData(LevelId.PumpkinHill)]
	[InlineData(LevelId.AquaticMine)]
	[InlineData(LevelId.DeathChamber)]
	[InlineData(LevelId.MeteorHerd)]
	[InlineData(LevelId.DryLagoon)]
	[InlineData(LevelId.EggQuarters)]
	[InlineData(LevelId.SecurityHall)]
	[InlineData(LevelId.MadSpace)]
	[InlineData(LevelId.Invalid)]
	public void ApplyDataDefaults_WritesCurrentLevel_ForEveryLevelId(LevelId levelId) {
		Reflect.Invoke(this.sa2, "ApplyDataDefaults", levelId, false, false, false);

		Assert.Equal((int)levelId, this.GetField().currentLevel);
		Assert.Equal((int)levelId, this.ReadFromAccessor().currentLevel);
		Assert.Equal((int)levelId, this.ReadIntField(nameof(HunterTeacherData.currentLevel)));
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, false, true)]
	[InlineData(false, true, false)]
	[InlineData(false, true, true)]
	[InlineData(true, false, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, false)]
	[InlineData(true, true, true)]
	public void ApplyDataDefaults_PropagatesAllBoolFlags(bool msp, bool backToMenu, bool timerReset) {
		Reflect.Invoke(this.sa2, "ApplyDataDefaults", LevelId.WildCanyon, msp, backToMenu, timerReset);

		HunterTeacherData fromField = this.GetField();
		Assert.Equal(msp, fromField.mspReversedHints);
		Assert.Equal(backToMenu, fromField.backToMenu);
		Assert.Equal(timerReset, fromField.timerReset);

		HunterTeacherData fromMemory = this.ReadFromAccessor();
		Assert.Equal(msp, fromMemory.mspReversedHints);
		Assert.Equal(backToMenu, fromMemory.backToMenu);
		Assert.Equal(timerReset, fromMemory.timerReset);
		Assert.Equal(msp, this.ReadBoolField(nameof(HunterTeacherData.mspReversedHints)));
		Assert.Equal(backToMenu, this.ReadBoolField(nameof(HunterTeacherData.backToMenu)));
		Assert.Equal(timerReset, this.ReadBoolField(nameof(HunterTeacherData.timerReset)));
	}

	[Fact]
	public void ApplyDataDefaults_ZerosPieceIds_AndClearsRunStateFlags() {
		this.SetField(new HunterTeacherData {
			p1Id = 0xAAAA,
			p2Id = 0xBBBB,
			p3Id = 0xCCCC,
			inWinScreen = true,
			sequenceComplete = true,
			levelLoading = true,
		});

		Reflect.Invoke(this.sa2, "ApplyDataDefaults", LevelId.WildCanyon, true, true, true);

		HunterTeacherData data = this.GetField();
		Assert.Equal(0, data.p1Id);
		Assert.Equal(0, data.p2Id);
		Assert.Equal(0, data.p3Id);
		Assert.False(data.inWinScreen);
		Assert.False(data.sequenceComplete);

		HunterTeacherData mem = this.ReadFromAccessor();
		Assert.Equal(0, mem.p1Id);
		Assert.Equal(0, mem.p2Id);
		Assert.Equal(0, mem.p3Id);
		Assert.False(mem.inWinScreen);
		Assert.False(mem.sequenceComplete);
		Assert.False(mem.levelLoading);

		Assert.Equal(0, this.ReadIntField(nameof(HunterTeacherData.p1Id)));
		Assert.Equal(0, this.ReadIntField(nameof(HunterTeacherData.p2Id)));
		Assert.Equal(0, this.ReadIntField(nameof(HunterTeacherData.p3Id)));
		Assert.False(this.ReadBoolField(nameof(HunterTeacherData.inWinScreen)));
		Assert.False(this.ReadBoolField(nameof(HunterTeacherData.sequenceComplete)));
		Assert.False(this.ReadBoolField(nameof(HunterTeacherData.levelLoading)));
	}

	[Fact]
	public void ApplyDataDefaults_DoesNotUseSeparateDefaultWriteHelper() {
		MethodInfo? method = typeof(SA2Manager).GetMethod("WriteHunterTeacherDataDefaults", BindingFlags.Instance | BindingFlags.NonPublic);

		Assert.Null(method);
	}

	#endregion

	#region ApplySet

	[Fact]
	public void ApplySet_UpdatesPendingData() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			WildCanyon level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			this.DriveSequenceToCompletion(level);
			this.SetField(new HunterTeacherData {
				inWinScreen = true,
				levelLoading = true,
				p1Id = 0x1111,
				p2Id = 0x2222,
				p3Id = 0x3333,
			});

			Set set = new(0xDEAD, 0xBEEF, 0xF00D, "left", "middle", "right");
			this.sa2.ApplySet(set, 1, 1, 1);

			HunterTeacherData data = this.GetField();
			Assert.Equal(0xDEAD, data.p1Id);
			Assert.Equal(0xBEEF, data.p2Id);
			Assert.Equal(0xF00D, data.p3Id);
			Assert.False(data.inWinScreen);
			Assert.False(data.levelLoading);
		});
	}

	[Fact]
	public void ApplySet_ThenWriteHunterTeacherData_WritesSetAndClearsConsumedEvents() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			WildCanyon level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			this.WriteBoolField(nameof(HunterTeacherData.inWinScreen), true);
			this.WriteBoolField(nameof(HunterTeacherData.levelLoading), true);
			object events = Reflect.Invoke(this.sa2, "ReadHunterTeacherData")!;

			Set set = new(0xDEAD, 0xBEEF, 0xF00D, "left", "middle", "right");
			this.sa2.ApplySet(set, 1, 1, 1);
			Reflect.Invoke(this.sa2, "WriteHunterTeacherData", events);

			Assert.False(this.ReadBoolField(nameof(HunterTeacherData.inWinScreen)));
			Assert.False(this.ReadBoolField(nameof(HunterTeacherData.levelLoading)));
			Assert.Equal(0xDEAD, this.ReadIntField(nameof(HunterTeacherData.p1Id)));
			Assert.Equal(0xBEEF, this.ReadIntField(nameof(HunterTeacherData.p2Id)));
			Assert.Equal(0xF00D, this.ReadIntField(nameof(HunterTeacherData.p3Id)));
		});
	}

	[Fact]
	public void ApplySet_SetsSequenceComplete_FromLevelSequenceWillBeComplete() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			WildCanyon level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			this.DriveSequenceToCompletion(level);

			Set set = new(1, 2, 3, "p1", "p2", "p3");
			this.sa2.ApplySet(set, 1, 1, 1);

			Assert.True(this.GetField().sequenceComplete);
		});
	}

	[Fact]
	public void ApplySet_SetsSequenceCompleteFalse_WhenLevelNotNearEnd() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			WildCanyon level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);

			Set set = new(1, 2, 3, "p1", "p2", "p3");
			this.sa2.ApplySet(set, 1, 1, 1);

			Assert.False(this.GetField().sequenceComplete);
		});
	}

	[Fact]
	public void ApplySet_LogsMessage_WhenSequenceNotComplete() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			WildCanyon level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			TextBox logBox = Reflect.GetStatic<TextBox>(typeof(HuntingTeacherForm), "CurrentLogBox")!;
			logBox.Clear();

			Set set = new(1, 2, 3, "alpha", "beta", "gamma");
			this.sa2.ApplySet(set, 4, 7, 2);

			Assert.Contains("Writing Set (4 / 7) For Rep (2):", logBox.Text);
			Assert.Contains("alpha", logBox.Text);
			Assert.Contains("beta", logBox.Text);
			Assert.Contains("gamma", logBox.Text);
		});
	}

	[Fact]
	public void ApplySet_DoesNotLog_WhenSequenceAlreadyComplete() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			WildCanyon level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			this.DriveSequenceToCompletion(level);
			TextBox logBox = Reflect.GetStatic<TextBox>(typeof(HuntingTeacherForm), "CurrentLogBox")!;
			logBox.Clear();

			Set set = new(1, 2, 3, "p1", "p2", "p3");
			this.sa2.ApplySet(set, 1, 1, 1);

			Assert.Empty(logBox.Text);
		});
	}

	[Theory]
	[InlineData(MspHints.ALTERNATING, false)]
	[InlineData(MspHints.ALTERNATING_REVERSED, true)]
	public void ApplySet_FirstRep_ForcesStartingOrientation_WhenAlternating(MspHints selection, bool expectedReversed) {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, selection);
			Reflect.SetField(this.sa2, "teacherForm", form);
			WildCanyon level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			this.DriveSequenceToCompletion(level);
			this.SetField(new HunterTeacherData { mspReversedHints = !expectedReversed });

			Set set = new(1, 2, 3, "p1", "p2", "p3");
			this.sa2.ApplySet(set, 1, 1, 1);

			Assert.Equal(expectedReversed, this.GetField().mspReversedHints);
		});
	}

	[Theory]
	[InlineData(MspHints.REVERSED)]
	[InlineData(MspHints.FIXED)]
	public void ApplySet_DoesNotToggleMspReversedHints_WhenNonAlternating(MspHints selection) {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, selection);
			Reflect.SetField(this.sa2, "teacherForm", form);
			WildCanyon level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			this.DriveSequenceToCompletion(level);
			this.SetField(new HunterTeacherData { mspReversedHints = true });

			Set set = new(1, 2, 3, "p1", "p2", "p3");
			this.sa2.ApplySet(set, 1, 1, 1);

			Assert.True(this.GetField().mspReversedHints);
		});
	}

	[Fact]
	public void ApplySet_AlternatesAcrossRepetitions_WithinSingleSet_WhenAlternating() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, MspHints.ALTERNATING);
			Reflect.SetField(this.sa2, "teacherForm", form);
			WildCanyon level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			this.DriveSequenceToCompletion(level);
			this.SetField(new HunterTeacherData { mspReversedHints = true });

			Set set = new(1, 2, 3, "p1", "p2", "p3");
			this.sa2.ApplySet(set, 1, 1, 1);
			bool afterRep1 = this.GetField().mspReversedHints;
			this.sa2.ApplySet(set, 1, 1, 2);
			bool afterRep2 = this.GetField().mspReversedHints;
			this.sa2.ApplySet(set, 1, 1, 3);
			bool afterRep3 = this.GetField().mspReversedHints;

			Assert.False(afterRep1);
			Assert.True(afterRep2);
			Assert.False(afterRep3);
		});
	}

	[Theory]
	[InlineData(MspHints.ALTERNATING)]
	[InlineData(MspHints.ALTERNATING_REVERSED)]
	public void ApplySet_NewSetFirstRep_MatchesPreviousSetFirstRep_WithEvenRepetitions(MspHints selection) {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, selection);
			Reflect.SetField(this.sa2, "teacherForm", form);
			MadSpace level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			this.DriveSequenceToCompletion(level);
			bool initialReversed = selection == MspHints.ALTERNATING;
			this.SetField(new HunterTeacherData { mspReversedHints = initialReversed });

			Set set = new(1, 2, 3, "p1", "p2", "p3");
			this.sa2.ApplySet(set, 1, 1, 1);
			bool firstSetFirstRep = this.GetField().mspReversedHints;
			this.sa2.ApplySet(set, 1, 1, 2);
			this.sa2.ApplySet(set, 1, 1, 1);
			bool secondSetFirstRep = this.GetField().mspReversedHints;

			Assert.Equal(firstSetFirstRep, secondSetFirstRep);
		});
	}

	[Theory]
	[InlineData(MspHints.ALTERNATING)]
	[InlineData(MspHints.ALTERNATING_REVERSED)]
	public void ApplySet_NewSetFirstRep_MatchesPreviousSetFirstRep_WithOddRepetitions(MspHints selection) {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, selection);
			Reflect.SetField(this.sa2, "teacherForm", form);
			MadSpace level = new(this.sa2, 1);
			Reflect.SetField(this.sa2, "level", level);
			this.DriveSequenceToCompletion(level);
			bool initialReversed = selection == MspHints.ALTERNATING;
			this.SetField(new HunterTeacherData { mspReversedHints = initialReversed });

			Set set = new(1, 2, 3, "p1", "p2", "p3");
			this.sa2.ApplySet(set, 1, 1, 1);
			bool firstSetFirstRep = this.GetField().mspReversedHints;
			this.sa2.ApplySet(set, 1, 1, 2);
			this.sa2.ApplySet(set, 1, 1, 3);
			this.sa2.ApplySet(set, 1, 1, 1);
			bool secondSetFirstRep = this.GetField().mspReversedHints;

			Assert.Equal(firstSetFirstRep, secondSetFirstRep);
		});
	}

	#endregion

	#region Shared memory field ownership

	[Fact]
	public void ReadHunterTeacherData_ReadsOnlyHelperOwnedEventFlags() {
		this.WriteBoolField(nameof(HunterTeacherData.inWinScreen), true);
		this.WriteBoolField(nameof(HunterTeacherData.levelLoading), true);
		this.WriteIntField(nameof(HunterTeacherData.p1Id), 0xAAAA);
		this.SetField(new HunterTeacherData { p1Id = 0x1111 });

		object? events = Reflect.Invoke(this.sa2, "ReadHunterTeacherData");

		HunterTeacherData data = this.GetField();
		Assert.NotNull(events);
		Assert.True(data.inWinScreen);
		Assert.True(data.levelLoading);
		Assert.Equal(0x1111, data.p1Id);
	}

	[Fact]
	public void WriteHunterTeacherData_WritesManagerOwnedFieldsWithoutClearingUnobservedEvents() {
		this.WriteBoolField(nameof(HunterTeacherData.inWinScreen), false);
		this.WriteBoolField(nameof(HunterTeacherData.levelLoading), false);
		object events = Reflect.Invoke(this.sa2, "ReadHunterTeacherData")!;
		this.WriteBoolField(nameof(HunterTeacherData.inWinScreen), true);
		this.WriteBoolField(nameof(HunterTeacherData.levelLoading), true);

		this.WriteBoolField(nameof(HunterTeacherData.sequenceComplete), false);
		this.WriteBoolField(nameof(HunterTeacherData.mspReversedHints), false);
		this.WriteIntField(nameof(HunterTeacherData.p1Id), 0x1111);
		this.WriteIntField(nameof(HunterTeacherData.p2Id), 0x2222);
		this.WriteIntField(nameof(HunterTeacherData.p3Id), 0x3333);

		this.SetField(new HunterTeacherData {
			inWinScreen = false,
			sequenceComplete = true,
			levelLoading = false,
			mspReversedHints = true,
			p1Id = 0xAAAA,
			p2Id = 0xBBBB,
			p3Id = 0xCCCC,
		});

		Reflect.Invoke(this.sa2, "WriteHunterTeacherData", events);

		Assert.True(this.ReadBoolField(nameof(HunterTeacherData.inWinScreen)));
		Assert.True(this.ReadBoolField(nameof(HunterTeacherData.levelLoading)));
		Assert.True(this.ReadBoolField(nameof(HunterTeacherData.sequenceComplete)));
		Assert.True(this.ReadBoolField(nameof(HunterTeacherData.mspReversedHints)));
		Assert.Equal(0xAAAA, this.ReadIntField(nameof(HunterTeacherData.p1Id)));
		Assert.Equal(0xBBBB, this.ReadIntField(nameof(HunterTeacherData.p2Id)));
		Assert.Equal(0xCCCC, this.ReadIntField(nameof(HunterTeacherData.p3Id)));
	}

	[Fact]
	public void WriteHunterTeacherData_ClearsOnlyEventsObservedByLastRead() {
		this.WriteBoolField(nameof(HunterTeacherData.inWinScreen), false);
		this.WriteBoolField(nameof(HunterTeacherData.levelLoading), true);
		object events = Reflect.Invoke(this.sa2, "ReadHunterTeacherData")!;
		this.WriteBoolField(nameof(HunterTeacherData.inWinScreen), true);

		HunterTeacherData data = this.GetField();
		data.inWinScreen = false;
		data.levelLoading = false;
		data.p1Id = 0x1000;
		this.SetField(data);

		Reflect.Invoke(this.sa2, "WriteHunterTeacherData", events);

		Assert.True(this.ReadBoolField(nameof(HunterTeacherData.inWinScreen)));
		Assert.False(this.ReadBoolField(nameof(HunterTeacherData.levelLoading)));
	}

	#endregion

	#region Simple getters

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void IsLevelLoading_ReturnsStructField(bool value) {
		this.SetField(new HunterTeacherData { levelLoading = value });
		Assert.Equal(value, this.sa2.IsLevelLoading());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void IsInWinScreen_ReturnsStructField(bool value) {
		this.SetField(new HunterTeacherData { inWinScreen = value });
		Assert.Equal(value, this.sa2.IsInWinScreen());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void RepetitionsInPlace_ReturnsBackingField(bool value) {
		Reflect.SetField(this.sa2, "repetitionsInPlace", value);
		Assert.Equal(value, this.sa2.RepetitionsInPlace());
	}

	#endregion

	#region LogMessage

	[Fact]
	public void LogMessage_AppendsTextToCurrentLogBox() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			TextBox logBox = Reflect.GetStatic<TextBox>(typeof(HuntingTeacherForm), "CurrentLogBox")!;
			logBox.Clear();

			this.sa2.LogMessage("hello world");

			Assert.Contains("hello world" + Environment.NewLine, logBox.Text);
		});
	}

	[Fact]
	public void LogMessage_NoOps_When_CurrentLogBox_IsNull() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			Reflect.SetStatic(typeof(HuntingTeacherForm), "CurrentLogBox", null);

			Exception? thrown = Record.Exception(() => this.sa2.LogMessage("anything"));

			Assert.Null(thrown);
		});
	}

	#endregion

	#region Dispose / CloseResource

	[Fact]
	public void Dispose_DisposesAndNullsStaticMemoryMapper() {
		MemoryMappedFile installed = MemoryMappedFile.CreateNew(null, Marshal.SizeOf<HunterTeacherData>());
		Reflect.SetStatic(typeof(SA2Manager), "MemoryMapper", installed);
		Reflect.SetField(this.sa2, "sa2", (IntPtr?)IntPtr.Zero);

		this.sa2.Dispose();

		Assert.Null(Reflect.GetStatic<MemoryMappedFile?>(typeof(SA2Manager), "MemoryMapper"));
		Assert.Throws<ObjectDisposedException>(() => installed.CreateViewAccessor());
	}

	[Fact]
	public void Dispose_NullsSa2Handle() {
		Reflect.SetStatic(typeof(SA2Manager), "MemoryMapper", null);
		Reflect.SetField(this.sa2, "sa2", (IntPtr?)IntPtr.Zero);

		this.sa2.Dispose();

		Assert.Null(Reflect.GetField<IntPtr?>(this.sa2, "sa2"));
	}

	[Fact]
	public void Dispose_IsIdempotent_WhenStaticAlreadyNull() {
		Reflect.SetStatic(typeof(SA2Manager), "MemoryMapper", null);
		Reflect.SetField(this.sa2, "sa2", (IntPtr?)null);

		this.sa2.Dispose();
		Exception? thrown = Record.Exception(() => this.sa2.Dispose());

		Assert.Null(thrown);
	}

	[Fact]
	public void CloseResource_NoOp_WhenSa2AlreadyNull() {
		Reflect.SetField(this.sa2, "sa2", (IntPtr?)null);

		Exception? thrown = Record.Exception(() => Reflect.Invoke(this.sa2, "CloseResource"));

		Assert.Null(thrown);
		Assert.Null(Reflect.GetField<IntPtr?>(this.sa2, "sa2"));
	}

	#endregion

	#region Static lifecycle

	[Fact]
	public void Stop_SetsCanRunFalse() {
		Reflect.SetStatic(typeof(SA2Manager), "CanRun", true);

		SA2Manager.Stop();

		Assert.False(Reflect.GetStatic<bool>(typeof(SA2Manager), "CanRun"));
	}

	[Fact]
	public void Start_SetsCanRunTrue_BeforeFailing_OnInvalidLevel() {
		Reflect.SetStatic(typeof(SA2Manager), "CanRun", false);
		HuntingTeacherForm uninitForm = (HuntingTeacherForm)RuntimeHelpers.GetUninitializedObject(typeof(HuntingTeacherForm));
		LevelRow row = new() { Level = (Level)999 };

		Assert.Throws<ArgumentException>(
			() => SA2Manager.Start(row, 1, uninitForm, false)
		);

		Assert.True(Reflect.GetStatic<bool>(typeof(SA2Manager), "CanRun"));
	}

	[Fact]
	public void Start_Throws_OnInvalidLevelEnum() {
		HuntingTeacherForm uninitForm = (HuntingTeacherForm)RuntimeHelpers.GetUninitializedObject(typeof(HuntingTeacherForm));
		LevelRow row = new() { Level = (Level)999 };

		ArgumentException ex = Assert.Throws<ArgumentException>(
			() => SA2Manager.Start(row, 1, uninitForm, false)
		);

		Assert.Equal("Invalid Level Selected!", ex.Message);
	}

	[Fact]
	public void Start_Throws_WhenSA2NotRunning_AndValidLevel() {
		if (Process.GetProcessesByName(SA2Manager.SONIC_EXECUTABLE).Length > 0) {
			return;
		}

		HuntingTeacherForm uninitForm = (HuntingTeacherForm)RuntimeHelpers.GetUninitializedObject(typeof(HuntingTeacherForm));
		LevelRow row = new() { Level = Level.WildCanyon };

		ArgumentException ex = Assert.Throws<ArgumentException>(
			() => SA2Manager.Start(row, 1, uninitForm, false)
		);

		Assert.Equal("SA2 Is Not Running!", ex.Message);
	}

	#endregion

	#region HunterTeacherData struct contract

	[Fact]
	public void HunterTeacherData_HasSequentialPack1Layout() {
		StructLayoutAttribute? layout = typeof(HunterTeacherData).StructLayoutAttribute;
		Assert.NotNull(layout);
		Assert.Equal(LayoutKind.Sequential, layout!.Value);
		Assert.Equal(1, layout.Pack);
	}

	[Fact]
	public void HunterTeacherData_HasCppCompatibleSize() {
		Assert.Equal(22, Marshal.SizeOf<HunterTeacherData>());
	}

	[Fact]
	public void HunterTeacherData_HasCppCompatibleFieldOffsets() {
		Dictionary<string, int> expected = new() {
			[nameof(HunterTeacherData.currentLevel)] = 0,
			[nameof(HunterTeacherData.inWinScreen)] = 4,
			[nameof(HunterTeacherData.sequenceComplete)] = 5,
			[nameof(HunterTeacherData.levelLoading)] = 6,
			[nameof(HunterTeacherData.mspReversedHints)] = 7,
			[nameof(HunterTeacherData.backToMenu)] = 8,
			[nameof(HunterTeacherData.timerReset)] = 9,
			[nameof(HunterTeacherData.p1Id)] = 10,
			[nameof(HunterTeacherData.p2Id)] = 14,
			[nameof(HunterTeacherData.p3Id)] = 18,
		};

		foreach ((string field, int offset) in expected) {
			Assert.Equal(offset, Marshal.OffsetOf<HunterTeacherData>(field).ToInt32());
		}
	}

	[Fact]
	public void SA2Manager_StoresSharedMemoryOffsetsInSingleLayoutObject() {
		FieldInfo field = typeof(SA2Manager).GetField("Offsets", BindingFlags.Static | BindingFlags.NonPublic)!;
		Assert.NotNull(field);

		object offsets = field.GetValue(null)!;
		string[] properties = offsets.GetType()
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Select(property => property.Name)
			.ToArray();

		Assert.Equal([
			"InWinScreen",
			"SequenceComplete",
			"LevelLoading",
			"MspReversedHints",
			"P1Id",
			"P2Id",
			"P3Id",
		], properties);
		Assert.Equal(4L, ReadOffset(offsets, "InWinScreen"));
		Assert.Equal(5L, ReadOffset(offsets, "SequenceComplete"));
		Assert.Equal(6L, ReadOffset(offsets, "LevelLoading"));
		Assert.Equal(7L, ReadOffset(offsets, "MspReversedHints"));
		Assert.Equal(10L, ReadOffset(offsets, "P1Id"));
		Assert.Equal(14L, ReadOffset(offsets, "P2Id"));
		Assert.Equal(18L, ReadOffset(offsets, "P3Id"));
	}

	[Fact]
	public void HunterTeacherData_HasExpectedFieldOrder() {
		string[] expected = [
			"currentLevel",
			"inWinScreen",
			"sequenceComplete",
			"levelLoading",
			"mspReversedHints",
			"backToMenu",
			"timerReset",
			"p1Id",
			"p2Id",
			"p3Id",
		];

		string[] actual = typeof(HunterTeacherData)
			.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
			.Select(f => f.Name)
			.ToArray();

		Assert.Equal(expected, actual);
	}

	[Fact]
	public void HunterTeacherData_RoundTripsThroughSharedMemory() {
		HunterTeacherData original = new() {
			currentLevel = (int)LevelId.MadSpace,
			inWinScreen = true,
			sequenceComplete = false,
			levelLoading = true,
			mspReversedHints = true,
			backToMenu = false,
			timerReset = true,
			p1Id = 0x1234,
			p2Id = 0x5678,
			p3Id = 0x9ABC,
		};

		this.accessor.Write(0, ref original);
		this.accessor.Read(0, out HunterTeacherData roundTripped);

		Assert.Equal(original.currentLevel, roundTripped.currentLevel);
		Assert.Equal(original.inWinScreen, roundTripped.inWinScreen);
		Assert.Equal(original.sequenceComplete, roundTripped.sequenceComplete);
		Assert.Equal(original.levelLoading, roundTripped.levelLoading);
		Assert.Equal(original.mspReversedHints, roundTripped.mspReversedHints);
		Assert.Equal(original.backToMenu, roundTripped.backToMenu);
		Assert.Equal(original.timerReset, roundTripped.timerReset);
		Assert.Equal(original.p1Id, roundTripped.p1Id);
		Assert.Equal(original.p2Id, roundTripped.p2Id);
		Assert.Equal(original.p3Id, roundTripped.p3Id);
	}

	#endregion

	#region Constants

	[Fact]
	public void Constants_AreUnchanged() {
		Assert.Equal("sonic2app", SA2Manager.SONIC_EXECUTABLE);
		Assert.Equal("hunting-teacher.helper.dll", SA2Manager.HELPER_DLL_NAME);
	}

	#endregion

	#region helpers

	private HunterTeacherData GetField() {
		return Reflect.GetField<HunterTeacherData>(this.sa2, "HunterTeacherData");
	}

	private void SetField(HunterTeacherData data) {
		Reflect.SetField(this.sa2, "HunterTeacherData", data);
	}

	private HunterTeacherData ReadFromAccessor() {
		this.accessor.Read(0, out HunterTeacherData data);
		return data;
	}

	private static long Offset(string field) {
		return Marshal.OffsetOf<HunterTeacherData>(field).ToInt64();
	}

	private bool ReadBoolField(string field) {
		return this.accessor.ReadBoolean(Offset(field));
	}

	private int ReadIntField(string field) {
		return this.accessor.ReadInt32(Offset(field));
	}

	private void WriteBoolField(string field, bool value) {
		this.accessor.Write(Offset(field), value);
	}

	private void WriteIntField(string field, int value) {
		this.accessor.Write(Offset(field), value);
	}

	private static long ReadOffset(object offsets, string propertyName) {
		PropertyInfo property = offsets.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)!;
		Assert.NotNull(property);
		return (long)property.GetValue(offsets)!;
	}

	private void DriveSequenceToCompletion(HuntingLevel level) {
		const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
		Set[] sequence = (Set[])typeof(HuntingLevel).GetProperty("Sequence", flags)!.GetValue(level)!;
		byte repetitions = (byte)typeof(HuntingLevel).GetProperty("Repetitions", flags)!.GetValue(level)!;
		Reflect.SetField(level, typeof(HuntingLevel), "SequenceCount", sequence.Length * repetitions);
	}

	private static HuntingTeacherForm BuildForm() {
		HuntingTeacherForm form = new();
		_ = form.Handle;
		return form;
	}

	private static void SetMspHints(HuntingTeacherForm form, MspHints value) {
		ComboBox combo = Reflect.GetField<ComboBox>(form, "mspHints");
		combo.SelectedValue = value;
	}

	#endregion
}
