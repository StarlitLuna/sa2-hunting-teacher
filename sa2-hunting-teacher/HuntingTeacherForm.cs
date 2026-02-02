using sa2_hunting_teacher.Updates;

namespace sa2_hunting_teacher;

public partial class HuntingTeacherForm : Form {
	private static TextBox? CurrentLogBox;
	private readonly Settings settings;
	private readonly UpdateManager updateManager;

	public sealed class LevelRow {
		public Level Level { get; init; } = default;
		public string Text { get; init; } = "";
		public string Group { get; init; } = "";
	}

	private static readonly Dictionary<Level, (string LevelText, string Category)> SupportedLevels = new() {
		/** Knuckles */
		{ Level.WildCanyon, ("Wild Canyon", "Knuckles") },
		{ Level.PumpkinHill, ("Pumpkin Hill", "Knuckles") },
		{ Level.AquaticMine, ("Aquatic Mine", "Knuckles") },
		{ Level.DeathChamber, ("Death Chamber", "Knuckles") },
		{ Level.MeteorHerd, ("Meteor Herd", "Knuckles") },
		/** Rouge */
		{ Level.DryLagoon, ("Dry Lagoon", "Rouge") },
		{ Level.EggQuarters, ("Egg Quarters", "Rouge") },
		{ Level.SecurityHall, ("Security Hall", "Rouge") },
		{ Level.MadSpace, ("Mad Space", "Rouge") },
	};

	public HuntingTeacherForm() {
		InitializeComponent();

		this.Text += " - v" + Application.ProductVersion;
		this.updateManager = new UpdateManager(this);
		HuntingTeacherForm.CurrentLogBox = this.logBox;
		this.levelSelector.DisplayMember = nameof(LevelRow.Text);
		this.levelSelector.ValueMember = nameof(LevelRow.Level);
		this.levelSelector.GroupMember = nameof(LevelRow.Group);
		this.levelSelector.SelectedValue = Level.WildCanyon;
		this.levelSelector.DataSource = HuntingTeacherForm.SupportedLevels.Select(kvp => new LevelRow {
			Level = kvp.Key,
			Text = kvp.Value.LevelText,
			Group = kvp.Value.Category
		}).ToList();

		this.settings = Settings.Load();
		InitializeSettings();
		InitializeTooltips();

		Task.Run(this.updateManager.CheckForUpdates);
	}

	private void InitializeSettings() {
		this.mspReverseHints.Checked = this.settings.MspReversedHints;
		this.backToMenu.Checked = this.settings.BackToMenu;
		this.timerReset.Checked = this.settings.TimerReset;
		this.inPlaceRepititions.Checked = this.settings.RepititionsInPlace;
		this.repetitions.Value = this.settings.Repititions;
	}

	private void InitializeTooltips() {
		this.reversedHintsTooltip.SetToolTip(
			this.mspReverseHints,
			"When enabled, hints in Mad Space will show up reversed (vanilla game behavior)\n" +
			"When disabled, hints in Mad Space will show up human-readable (modded game behavior)"
		);

		this.backToMenuTooltip.SetToolTip(
			this.backToMenu,
			"When enabled, you will go back to stage select after collecting your last piece\n" +
			"When disabled, you will instantly respawn to your next set without having to go back to stage select"
		);

		this.timerResetTooltip.SetToolTip(
			this.timerReset,
			"When enabled, the in-game timer will reset inbetween sets after you collect your third piece\n" +
			"When disabled, the in-game timer will continue counting inbetween sets\n" +
			"This setting does nothing when 'Back To Menu' is enabled"
		);

		this.inPlaceRepititionsTooltip.SetToolTip(
			this.inPlaceRepititions,
			"When enabled, you will play a set a 'repitition' number of times before proceeding to the next set\n" +
			"When disabled, you will play a set once, proceed to the next set, then the sequence will repeat a 'repitition' number of times"
		);
	}

	private void SaveSettings() {
		this.settings.MspReversedHints = this.mspReverseHints.Checked;
		this.settings.BackToMenu = this.backToMenu.Checked;
		this.settings.TimerReset = this.timerReset.Checked;
		this.settings.RepititionsInPlace = this.inPlaceRepititions.Checked;
		this.settings.Repititions = (byte)this.repetitions.Value;
		this.settings.Save();
	}

	public static void AddLogItem(string value) {
		if (HuntingTeacherForm.CurrentLogBox == null) {
			return;
		}

		HuntingTeacherForm.CurrentLogBox.AppendText(value + Environment.NewLine);
	}
	public bool MspReversedHints() {
		return this.mspReverseHints.Checked;
	}

	public bool BackToMenu() {
		return this.backToMenu.Checked;
	}

	public bool TimerReset() {
		return this.timerReset.Checked;
	}

	public bool RepititionsInPlace() {
		return this.inPlaceRepititions.Checked;
	}

	private void StartBtn_Click(object sender, EventArgs e) {
		Level selectedLevel = (Level)this.levelSelector.SelectedValue!;
		this.startBtn.Enabled = false;
		this.repetitions.Enabled = false;
		this.mspReverseHints.Enabled = false;
		this.backToMenu.Enabled = false;
		this.timerReset.Enabled = false;
		this.inPlaceRepititions.Enabled = false;
		this.resetBtn.Enabled = true;

		Task.Run(() => {
			try {
				SA2Manager.Start(selectedLevel, (byte)this.repetitions.Value, this, this.inPlaceRepititions.Checked);
			} catch (ArgumentException) {
				this.Invoke(() => {
					MessageBox.Show(this, "A running instance of SA2 could not be found.\n" +
						"Make sure SA2 is running first before trying to start a level.", "SA2 Not Running", MessageBoxButtons.OK, MessageBoxIcon.Error);

					this.ResetBtn_Click(sender, e);
				});
			}
		});
	}

	public void ResetBtn_Click(object sender, EventArgs e) {
		SA2Manager.Stop();
		this.resetBtn.Enabled = false;
		this.repetitions.Enabled = true;
		this.mspReverseHints.Enabled = this.ShouldEnableMspReverseHints();
		this.backToMenu.Enabled = true;
		this.timerReset.Enabled = true;
		this.inPlaceRepititions.Enabled = true;
		this.startBtn.Enabled = true;
	}

	private bool ShouldEnableMspReverseHints() {
		return this.levelSelector.SelectedItem != null && ((LevelRow)this.levelSelector.SelectedItem).Level == Level.MadSpace;
	}

	private void LevelSelector_SelectedIndexChanged(object sender, EventArgs e) {
		this.mspReverseHints.Enabled = this.ShouldEnableMspReverseHints();
	}

	private void SettingsChanged(object sender, EventArgs e) {
		this.SaveSettings();
	}

	private void setEditor_Click(object sender, EventArgs e) {
		SetEditor editorForm = new();
		editorForm.ShowDialog(this);
	}
}
