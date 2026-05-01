using sa2_hunting_teacher;
using System.Windows.Forms;

namespace unit_tests;

[Collection(StaticStateCollection.Name)]
public class HuntingTeacherFormTests : IDisposable {
	private readonly bool appDataDirExisted;
	private readonly bool settingsFileExisted;
	private readonly byte[]? backupContent;
	private readonly TextBox? initialLogBox;

	public HuntingTeacherFormTests() {
		this.appDataDirExisted = Directory.Exists(Settings.AppDataPath);
		this.settingsFileExisted = File.Exists(Settings.SettingsPath);
		if (this.settingsFileExisted) {
			this.backupContent = File.ReadAllBytes(Settings.SettingsPath);
		}
		this.initialLogBox = Reflect.GetStatic<TextBox?>(typeof(HuntingTeacherForm), "CurrentLogBox");

		if (File.Exists(Settings.SettingsPath)) {
			File.Delete(Settings.SettingsPath);
		}
	}

	public void Dispose() {
		Reflect.SetStatic(typeof(HuntingTeacherForm), "CurrentLogBox", this.initialLogBox);

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

	[Theory]
	[InlineData(MspHints.ALTERNATING)]
	[InlineData(MspHints.ALTERNATING_REVERSED)]
	public void MspHintsChanged_ForcesInPlaceRepetitions_WhenAlternating(MspHints alternating) {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, MspHints.REVERSED);
			SetInPlaceRepetitions(form, false);

			SetMspHints(form, alternating);

			Assert.True(GetInPlaceRepetitions(form));
			Assert.Equal(2, GetRepetitionsMinimum(form));
		});
	}

	[Fact]
	public void MspHintsChanged_RaisesRepetitionsToTwo_WhenBelowTwoAndAlternating() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, MspHints.REVERSED);
			SetRepetitions(form, 1);

			SetMspHints(form, MspHints.ALTERNATING);

			Assert.Equal(2, GetRepetitions(form));
		});
	}

	[Fact]
	public void MspHintsChanged_LeavesRepetitionsAlone_WhenAlreadyAtLeastTwoAndAlternating() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, MspHints.REVERSED);
			SetRepetitions(form, 5);

			SetMspHints(form, MspHints.ALTERNATING);

			Assert.Equal(5, GetRepetitions(form));
		});
	}

	[Theory]
	[InlineData(MspHints.REVERSED)]
	[InlineData(MspHints.FIXED)]
	public void MspHintsChanged_RestoresMinimumOfOne_WhenNonAlternating(MspHints nonAlternating) {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, MspHints.ALTERNATING);

			SetMspHints(form, nonAlternating);

			Assert.Equal(1, GetRepetitionsMinimum(form));
		});
	}

	[Fact]
	public void MspHintsChanged_PersistsSelectionToSettingsFile() {
		StaHelper.RunSta(() => {
			using HuntingTeacherForm form = BuildForm();
			SetMspHints(form, MspHints.REVERSED);

			SetMspHints(form, MspHints.ALTERNATING_REVERSED);

			Settings loaded = Settings.Load();
			Assert.Equal(MspHints.ALTERNATING_REVERSED, loaded.MspHints);
		});
	}

	private static HuntingTeacherForm BuildForm() {
		HuntingTeacherForm form = new();
		_ = form.Handle;
		return form;
	}

	private static void SetMspHints(HuntingTeacherForm form, MspHints value) {
		Reflect.GetField<ComboBox>(form, "mspHints").SelectedValue = value;
	}

	private static void SetInPlaceRepetitions(HuntingTeacherForm form, bool value) {
		Reflect.GetField<CheckBox>(form, "inPlaceRepetitions").Checked = value;
	}

	private static bool GetInPlaceRepetitions(HuntingTeacherForm form) {
		return Reflect.GetField<CheckBox>(form, "inPlaceRepetitions").Checked;
	}

	private static void SetRepetitions(HuntingTeacherForm form, int value) {
		Reflect.GetField<NumericUpDown>(form, "repetitions").Value = value;
	}

	private static int GetRepetitions(HuntingTeacherForm form) {
		return (int)Reflect.GetField<NumericUpDown>(form, "repetitions").Value;
	}

	private static int GetRepetitionsMinimum(HuntingTeacherForm form) {
		return (int)Reflect.GetField<NumericUpDown>(form, "repetitions").Minimum;
	}
}
