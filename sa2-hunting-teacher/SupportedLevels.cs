using sa2_hunting_teacher.DropdownControls;

namespace sa2_hunting_teacher;

public sealed class LevelRow {
	public Level Level { get; init; } = default;
	public string Text { get; init; } = "";
	public string Group { get; init; } = "";
}

internal static class SupportedLevels {
	public static readonly Dictionary<Level, (string LevelText, string Category)> Map = new() {
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

	public static void Configure(GroupedComboBox combo, Level? initialSelection = null) {
		combo.DisplayMember = nameof(LevelRow.Text);
		combo.ValueMember = nameof(LevelRow.Level);
		combo.GroupMember = nameof(LevelRow.Group);
		combo.DataSource = SupportedLevels.Map.Select(kvp => new LevelRow {
			Level = kvp.Key,
			Text = kvp.Value.LevelText,
			Group = kvp.Value.Category
		}).ToList();

		if (initialSelection.HasValue) {
			combo.SelectedValue = initialSelection.Value;
		}
	}
}
