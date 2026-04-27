using sa2_hunting_teacher.Knuckles;
using sa2_hunting_teacher.Rouge;

namespace sa2_hunting_teacher;

public sealed class LevelCatalog {
	public required IReadOnlyList<int> P1 { get; init; }
	public required IReadOnlyList<int> P2 { get; init; }
	public required IReadOnlyList<int> P3 { get; init; }
	public required IReadOnlyList<int> Enemies { get; init; }
	public required IReadOnlyDictionary<int, string> Hints { get; init; }

	public string HintFor(int id) {
		if (this.Hints.TryGetValue(id, out string? hint)) {
			return hint;
		}

		return $"Piece 0x{id:X4}";
	}

	private static readonly Dictionary<Level, LevelCatalog> Map = new() {
		{ Level.WildCanyon, WildCanyon.Catalog },
		{ Level.PumpkinHill, PumpkinHill.Catalog },
		{ Level.AquaticMine, AquaticMine.Catalog },
		{ Level.DeathChamber, DeathChamber.Catalog },
		{ Level.MeteorHerd, MeteorHerd.Catalog },
		{ Level.DryLagoon, DryLagoon.Catalog },
		{ Level.EggQuarters, EggQuarters.Catalog },
		{ Level.SecurityHall, SecurityHall.Catalog },
		{ Level.MadSpace, MadSpace.Catalog }
	};

	public static LevelCatalog Get(Level level) {
		return LevelCatalog.Map[level];
	}

	internal static LevelCatalog Build<TP1, TP2, TP3, TEnemy>(
		IReadOnlyDictionary<int, string> hints,
		IReadOnlyDictionary<int, string> impossibleHints
	)
		where TP1 : struct, Enum
		where TP2 : struct, Enum
		where TP3 : struct, Enum
		where TEnemy : struct, Enum {
		Dictionary<int, string> merged = new(hints);
		foreach (KeyValuePair<int, string> kvp in impossibleHints) {
			if (!merged.ContainsKey(kvp.Key)) {
				merged[kvp.Key] = kvp.Value;
			}
		}

		LevelCatalog.MergeEnumNames<TP1>(merged);
		LevelCatalog.MergeEnumNames<TP2>(merged);
		LevelCatalog.MergeEnumNames<TP3>(merged);
		LevelCatalog.MergeEnumNames<TEnemy>(merged);

		return new LevelCatalog {
			P1 = LevelCatalog.EnumIds<TP1>(),
			P2 = LevelCatalog.EnumIds<TP2>(),
			P3 = LevelCatalog.EnumIds<TP3>(),
			Enemies = LevelCatalog.EnumIds<TEnemy>(),
			Hints = merged
		};
	}

	private static int[] EnumIds<T>() where T : struct, Enum {
		T[] values = Enum.GetValues<T>();
		int[] ids = new int[values.Length];
		for (int i = 0; i < values.Length; i++) {
			ids[i] = Set.EnumKey(values[i]);
		}

		return ids;
	}

	private static void MergeEnumNames<T>(Dictionary<int, string> hints) where T : struct, Enum {
		foreach (T value in Enum.GetValues<T>()) {
			int id = Set.EnumKey(value);
			if (!hints.ContainsKey(id)) {
				hints[id] = value.ToString();
			}
		}
	}
}
