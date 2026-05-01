using sa2_hunting_teacher;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace unit_tests;

[Collection(StaticStateCollection.Name)]
public class HuntingLevelTests : IDisposable {
	private readonly SA2Manager sa2;
	private readonly MemoryMappedFile mmf;
	private readonly MemoryMappedViewAccessor accessor;
	private readonly bool initialCanRun;
	private readonly MemoryMappedFile? initialMapper;
	private readonly TextBox? initialLogBox;

	public HuntingLevelTests() {
		this.initialCanRun = Reflect.GetStatic<bool>(typeof(SA2Manager), "CanRun");
		this.initialMapper = Reflect.GetStatic<MemoryMappedFile?>(typeof(SA2Manager), "MemoryMapper");
		this.initialLogBox = Reflect.GetStatic<TextBox?>(typeof(HuntingTeacherForm), "CurrentLogBox");

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
			try { leaked.Dispose(); } catch { }
		}
		Reflect.SetStatic(typeof(SA2Manager), "MemoryMapper", this.initialMapper);
		Reflect.SetStatic(typeof(SA2Manager), "CanRun", this.initialCanRun);
		Reflect.SetStatic(typeof(HuntingTeacherForm), "CurrentLogBox", this.initialLogBox);

		this.accessor.Dispose();
		this.mmf.Dispose();
		GC.SuppressFinalize(this);
	}

	#region SequenceComplete

	[Theory]
	[InlineData(3, (byte)1, 0, false)]
	[InlineData(3, (byte)1, 2, false)]
	[InlineData(3, (byte)1, 3, true)]
	[InlineData(3, (byte)1, 4, true)]
	[InlineData(5, (byte)2, 9, false)]
	[InlineData(5, (byte)2, 10, true)]
	[InlineData(5, (byte)2, 11, true)]
	[InlineData(1, (byte)1, 0, false)]
	[InlineData(1, (byte)1, 1, true)]
	public void SequenceComplete_TrueWhenCountReachesLengthTimesRepetitions(
		int sequenceLength, byte repetitions, int sequenceCount, bool expected
	) {
		TestLevel level = this.CreateLevel(repetitions, sequenceLength);
		SetSequenceCount(level, sequenceCount);

		Assert.Equal(expected, level.SequenceComplete());
	}

	#endregion

	#region SequenceWillBeComplete

	[Theory]
	[InlineData(3, (byte)1, 0, false)]
	[InlineData(3, (byte)1, 1, false)]
	[InlineData(3, (byte)1, 2, true)]
	[InlineData(3, (byte)1, 3, true)]
	[InlineData(5, (byte)2, 8, false)]
	[InlineData(5, (byte)2, 9, true)]
	[InlineData(5, (byte)2, 10, true)]
	[InlineData(1, (byte)1, 0, true)]
	public void SequenceWillBeComplete_TrueAtOrAfterCountEqualsLengthTimesRepetitionsMinusOne(
		int sequenceLength, byte repetitions, int sequenceCount, bool expected
	) {
		TestLevel level = this.CreateLevel(repetitions, sequenceLength);
		SetSequenceCount(level, sequenceCount);

		Assert.Equal(expected, level.SequenceWillBeComplete());
	}

	#endregion

	#region RunSequence — gating on win/loading state

	[Fact]
	public void RunSequence_NoOp_WhenNotInWinScreenAndNotLevelLoading() {
		TestLevel level = this.CreateLevel(1, 3);
		this.SetState(inWin: false, levelLoading: false);

		level.RunSequence();

		HunterTeacherData data = this.GetData();
		Assert.Equal(0, data.p1Id);
		Assert.Equal(0, data.p2Id);
		Assert.Equal(0, data.p3Id);
		Assert.Equal(0, GetSequenceCount(level));
		Assert.Equal(0, GetNext(level));
	}

	[Fact]
	public void RunSequence_AppliesCurrentSet_WhenLevelLoadingOnly_WithoutAdvancingSequenceCountOrNext() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			TestLevel level = this.CreateLevel(1, 3);
			SetSequenceCount(level, 100);
			this.SetState(inWin: false, levelLoading: true);

			level.RunSequence();

			HunterTeacherData data = this.GetData();
			Assert.Equal(0x1000, data.p1Id);
			Assert.Equal(0x2000, data.p2Id);
			Assert.Equal(0x3000, data.p3Id);
			Assert.Equal(100, GetSequenceCount(level));
			Assert.Equal(0, GetNext(level));
		});
	}

	[Fact]
	public void RunSequence_AdvancesAndAppliesNextSet_WhenInWinScreen() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			TestLevel level = this.CreateLevel(1, 3);
			SetSequenceCount(level, 100);
			SetNext(level, 0);
			this.SetState(inWin: true, levelLoading: false);

			level.RunSequence();

			HunterTeacherData data = this.GetData();
			Assert.Equal(0x1001, data.p1Id);
			Assert.Equal(1, GetNext(level));
			Assert.Equal(101, GetSequenceCount(level));
		});
	}

	[Fact]
	public void RunSequence_PrefersWinScreenAdvance_WhenBothFlagsAreSet() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			TestLevel level = this.CreateLevel(1, 3);
			SetSequenceCount(level, 100);
			SetNext(level, 0);
			this.SetState(inWin: true, levelLoading: true);

			level.RunSequence();

			Assert.Equal(0x1001, this.GetData().p1Id);
			Assert.Equal(1, GetNext(level));
			Assert.Equal(101, GetSequenceCount(level));
		});
	}

	#endregion

	#region RunSequence — standard (not in-place) repetition mode

	[Fact]
	public void RunSequence_StandardMode_WrapsToZeroAfterLastSet() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			TestLevel level = this.CreateLevel(2, 3);
			Reflect.SetField(this.sa2, "repetitionsInPlace", false);
			SetSequenceCount(level, 100);
			SetNext(level, 2);
			this.SetState(inWin: true, levelLoading: false);

			level.RunSequence();

			Assert.Equal(0x1000, this.GetData().p1Id);
			Assert.Equal(0, GetNext(level));
		});
	}

	[Fact]
	public void RunSequence_StandardMode_AdvancesOneStepAtATime() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			TestLevel level = this.CreateLevel(2, 4);
			Reflect.SetField(this.sa2, "repetitionsInPlace", false);
			SetSequenceCount(level, 100);
			SetNext(level, 1);
			this.SetState(inWin: true, levelLoading: false);

			level.RunSequence();

			Assert.Equal(0x1002, this.GetData().p1Id);
			Assert.Equal(2, GetNext(level));
		});
	}

	[Fact]
	public void RunSequence_StandardMode_LogsSetNumberAndCurrentRep() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);

			TestLevel level = this.CreateLevel(3, 3);
			Reflect.SetField(this.sa2, "repetitionsInPlace", false);
			this.SetState(inWin: true, levelLoading: false);
			TextBox logBox = ClearLogBox();

			level.RunSequence();

			Assert.Contains("(2 / 3)", logBox.Text);
			Assert.Contains("For Rep (1):", logBox.Text);
		});
	}

	[Fact]
	public void RunSequence_StandardMode_AdvancesCurrentRepAfterFullSequenceWrap() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);

			TestLevel level = this.CreateLevel(3, 3);
			Reflect.SetField(this.sa2, "repetitionsInPlace", false);
			SetSequenceCount(level, 3);
			SetNext(level, 2);
			this.SetState(inWin: true, levelLoading: false);
			TextBox logBox = ClearLogBox();

			level.RunSequence();

			Assert.Contains("For Rep (2):", logBox.Text);
		});
	}

	#endregion

	#region RunSequence — in-place repetition mode

	[Fact]
	public void RunSequence_RepetitionsInPlace_RepeatsCurrentSetUntilRepsExhausted() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			TestLevel level = this.CreateLevel(2, 3);
			Reflect.SetField(this.sa2, "repetitionsInPlace", true);
			SetSequenceCount(level, 100);
			SetNext(level, 0);
			SetRepetition(level, 0);
			this.SetState(inWin: true, levelLoading: false);

			level.RunSequence();

			Assert.Equal(0x1000, this.GetData().p1Id);
			Assert.Equal(0, GetNext(level));
			Assert.Equal((byte)1, GetRepetition(level));
		});
	}

	[Fact]
	public void RunSequence_RepetitionsInPlace_AdvancesAfterAllRepetitionsForCurrentSet() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			TestLevel level = this.CreateLevel(2, 3);
			Reflect.SetField(this.sa2, "repetitionsInPlace", true);
			SetSequenceCount(level, 100);
			SetNext(level, 0);
			SetRepetition(level, 1);
			this.SetState(inWin: true, levelLoading: false);

			level.RunSequence();

			Assert.Equal(0x1001, this.GetData().p1Id);
			Assert.Equal(1, GetNext(level));
			Assert.Equal((byte)0, GetRepetition(level));
		});
	}

	[Fact]
	public void RunSequence_RepetitionsInPlace_WrapsToFirstSetAfterLastSetCompleted() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);
			TestLevel level = this.CreateLevel(2, 3);
			Reflect.SetField(this.sa2, "repetitionsInPlace", true);
			SetSequenceCount(level, 100);
			SetNext(level, 2);
			SetRepetition(level, 1);
			this.SetState(inWin: true, levelLoading: false);

			level.RunSequence();

			Assert.Equal(0x1000, this.GetData().p1Id);
			Assert.Equal(0, GetNext(level));
			Assert.Equal((byte)0, GetRepetition(level));
		});
	}

	[Fact]
	public void RunSequence_RepetitionsInPlace_LogsRepetitionPlusOneAsCurrentRep() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);

			TestLevel level = this.CreateLevel(3, 3);
			Reflect.SetField(this.sa2, "repetitionsInPlace", true);
			this.SetState(inWin: true, levelLoading: false);
			TextBox logBox = ClearLogBox();

			level.RunSequence();

			Assert.Contains("For Rep (2):", logBox.Text);
		});
	}

	[Fact]
	public void RunSequence_LevelLoading_ReportsCurrentRepBasedOnInitialState() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);

			TestLevel level = this.CreateLevel(3, 5);
			Reflect.SetField(this.sa2, "repetitionsInPlace", true);
			this.SetState(inWin: false, levelLoading: true);
			TextBox logBox = ClearLogBox();

			level.RunSequence();

			Assert.Contains("(1 / 5) For Rep (1):", logBox.Text);
		});
	}

	#endregion

	#region RunSequence — set/total bookkeeping

	[Fact]
	public void RunSequence_PassesSequenceLengthAsSeqTotal() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);

			TestLevel level = this.CreateLevel(1, 7);
			this.SetState(inWin: true, levelLoading: false);
			TextBox logBox = ClearLogBox();

			level.RunSequence();

			Assert.Contains("/ 7)", logBox.Text);
		});
	}

	[Fact]
	public void RunSequence_PassesNextPlusOneAsSeqCount() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			Reflect.SetField(this.sa2, "teacherForm", form);

			TestLevel level = this.CreateLevel(1, 5);
			SetNext(level, 2);
			this.SetState(inWin: true, levelLoading: false);
			TextBox logBox = ClearLogBox();

			level.RunSequence();

			Assert.Contains("(4 / 5)", logBox.Text);
		});
	}

	#endregion

	#region helpers

	private TestLevel CreateLevel(byte repetitions, int sequenceLength) {
		Set[] sets = new Set[sequenceLength];
		for (int i = 0; i < sequenceLength; i++) {
			sets[i] = new Set(0x1000 + i, 0x2000 + i, 0x3000 + i, $"P1-{i}", $"P2-{i}", $"P3-{i}");
		}
		TestLevel level = new(this.sa2, repetitions, sets);
		Reflect.SetField(this.sa2, "level", level);
		return level;
	}

	private void SetState(bool inWin, bool levelLoading) {
		Reflect.SetField(this.sa2, "HunterTeacherData", new HunterTeacherData {
			inWinScreen = inWin,
			levelLoading = levelLoading,
		});
	}

	private HunterTeacherData GetData() {
		return Reflect.GetField<HunterTeacherData>(this.sa2, "HunterTeacherData");
	}

	private static int GetSequenceCount(HuntingLevel level) {
		return Reflect.GetField<int>(level, typeof(HuntingLevel), "SequenceCount");
	}

	private static void SetSequenceCount(HuntingLevel level, int count) {
		Reflect.SetField(level, typeof(HuntingLevel), "SequenceCount", count);
	}

	private static int GetNext(HuntingLevel level) {
		return Reflect.GetField<int>(level, typeof(HuntingLevel), "Next");
	}

	private static void SetNext(HuntingLevel level, int next) {
		Reflect.SetField(level, typeof(HuntingLevel), "Next", next);
	}

	private static byte GetRepetition(HuntingLevel level) {
		PropertyInfo prop = typeof(HuntingLevel).GetProperty("Repetition", BindingFlags.Instance | BindingFlags.NonPublic)!;
		return (byte)prop.GetValue(level)!;
	}

	private static void SetRepetition(HuntingLevel level, byte repetition) {
		PropertyInfo prop = typeof(HuntingLevel).GetProperty("Repetition", BindingFlags.Instance | BindingFlags.NonPublic)!;
		prop.SetValue(level, repetition);
	}

	private static HuntingTeacherForm BuildForm() {
		HuntingTeacherForm form = new();
		_ = form.Handle;
		return form;
	}

	private static TextBox ClearLogBox() {
		TextBox logBox = Reflect.GetStatic<TextBox>(typeof(HuntingTeacherForm), "CurrentLogBox")!;
		logBox.Clear();
		return logBox;
	}

	#endregion

	internal class TestLevel(SA2Manager manager, byte repetitions, Set[] sequence)
		: HuntingLevel(manager, repetitions) {
		public override LevelId LevelId => LevelId.WildCanyon;
		protected override Set[] Sequence { get; } = sequence;
		public override Dictionary<int, string> PieceToHintInstance { get; } = new();
	}
}
