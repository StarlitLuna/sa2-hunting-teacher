using System.Collections;
using sa2_hunting_teacher.DropdownControls;

namespace sa2_hunting_teacher;

public sealed class LevelRow {
	public Level Level { get; init; } = default;
	public string Text { get; init; } = "";
	public string Group { get; init; } = "";
	public PersistedSequence? CustomSequence { get; init; }
}

internal static class SupportedLevels {
	public const string CustomGroup = "Custom";

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

	private static readonly Dictionary<string, int> GroupRank = new() {
		{ "Knuckles", 0 },
		{ "Rouge", 1 },
		{ SupportedLevels.CustomGroup, 2 }
	};

	private static readonly IComparer GroupRankComparer = new GroupRankComparerImpl();

	public static void Configure(
		GroupedComboBox combo,
		IEnumerable<PersistedSequence>? customSequences = null,
		Level? initialSelection = null
	) {
		List<LevelRow> rows = new();
		foreach (KeyValuePair<Level, (string LevelText, string Category)> kvp in SupportedLevels.Map) {
			rows.Add(new LevelRow {
				Level = kvp.Key,
				Text = kvp.Value.LevelText,
				Group = kvp.Value.Category
			});
		}

		if (customSequences != null) {
			foreach (PersistedSequence custom in customSequences) {
				rows.Add(new LevelRow {
					Level = custom.Level,
					Text = custom.Name,
					Group = SupportedLevels.CustomGroup,
					CustomSequence = custom
				});
			}
		}

		combo.DataSource = null;
		combo.SortComparer = SupportedLevels.GroupRankComparer;
		combo.DisplayMember = nameof(LevelRow.Text);
		combo.ValueMember = nameof(LevelRow.Level);
		combo.GroupMember = nameof(LevelRow.Group);
		combo.DataSource = rows;

		if (initialSelection.HasValue) {
			combo.SelectedValue = initialSelection.Value;
		}
	}

	private sealed class GroupRankComparerImpl : IComparer {
		public int Compare(object? x, object? y) {
			if (x is string sx && y is string sy
				&& SupportedLevels.GroupRank.TryGetValue(sx, out int rx)
				&& SupportedLevels.GroupRank.TryGetValue(sy, out int ry)) {
				return rx.CompareTo(ry);
			}

			return Comparer.Default.Compare(x, y);
		}
	}
}
