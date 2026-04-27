using sa2_hunting_teacher;
using System.Collections;
using System.Reflection;

namespace unit_tests;

public class SetEditorDropdownTests {
	private static readonly Type SetEditorType = typeof(SetEditor);
	private static readonly Type SlotType =
		SetEditorType.GetNestedType("Slot", BindingFlags.NonPublic)!;
	private static readonly Type CustomSetType =
		SetEditorType.GetNestedType("CustomSet", BindingFlags.NonPublic)!;
	private static readonly Type PieceOptionType =
		SetEditorType.GetNestedType("PieceOption", BindingFlags.NonPublic)!;
	private static readonly Type NoneOptionType =
		SetEditorType.GetNestedType("NoneOption", BindingFlags.NonPublic)!;
	private static readonly MethodInfo BuildOptionsMethod =
		SetEditorType.GetMethod("BuildOptions", BindingFlags.NonPublic | BindingFlags.Static)!;
	private static readonly PropertyInfo PieceOptionId =
		PieceOptionType.GetProperty("Id")!;
	private static readonly PropertyInfo PieceOptionHint =
		PieceOptionType.GetProperty("Hint")!;

	[Fact]
	public void Dropdown_EveryOptionHintMatchesItsPieceId_ForAllLevelsAndSlots() {
		foreach (Level level in Enum.GetValues<Level>()) {
			LevelCatalog catalog = LevelCatalog.Get(level);
			foreach (object slot in Enum.GetValues(SlotType)) {
				IReadOnlyList<int> ownIds = OwnIdsForSlot(catalog, slot);
				HashSet<int> expectedIds = new(ownIds);
				foreach (int id in catalog.Enemies) {
					expectedIds.Add(id);
				}

				IList items = InvokeBuildOptions(catalog, slot, NewEmptySet());

				Assert.NotEmpty(items);
				Assert.IsType(NoneOptionType, items[0]!);

				HashSet<int> seenIds = new();
				for (int i = 1; i < items.Count; i++) {
					object item = items[i]!;
					Assert.IsType(PieceOptionType, item);

					int id = (int)PieceOptionId.GetValue(item)!;
					string hint = (string)PieceOptionHint.GetValue(item)!;

					Assert.Equal(catalog.HintFor(id), hint);
					Assert.Equal(hint, item.ToString());
					Assert.Contains(id, expectedIds);
					Assert.True(
						seenIds.Add(id),
						$"Duplicate piece id 0x{id:X4} in {level}/{slot} dropdown."
					);
				}

				Assert.Equal(expectedIds, seenIds);
			}
		}
	}

	[Fact]
	public void Dropdown_ExcludesEnemyAlreadyUsedInAnotherSlot() {
		foreach (Level level in Enum.GetValues<Level>()) {
			LevelCatalog catalog = LevelCatalog.Get(level);
			if (catalog.Enemies.Count == 0) {
				continue;
			}

			int placedEnemy = catalog.Enemies[0];
			object slotP2 = Enum.Parse(SlotType, "P2");
			object slotP1 = Enum.Parse(SlotType, "P1");
			object model = NewSet(p1: null, p2: placedEnemy, p3: null);

			IList items = InvokeBuildOptions(catalog, slotP1, model);

			HashSet<int> seenIds = new();
			for (int i = 1; i < items.Count; i++) {
				int id = (int)PieceOptionId.GetValue(items[i]!)!;
				seenIds.Add(id);
			}

			Assert.DoesNotContain(placedEnemy, seenIds);

			foreach (int otherEnemy in catalog.Enemies) {
				if (otherEnemy == placedEnemy) {
					continue;
				}

				Assert.Contains(otherEnemy, seenIds);
			}
		}
	}

	private static IReadOnlyList<int> OwnIdsForSlot(LevelCatalog catalog, object slot) {
		string name = slot.ToString()!;
		return name switch {
			"P1" => catalog.P1,
			"P2" => catalog.P2,
			"P3" => catalog.P3,
			_ => throw new ArgumentOutOfRangeException(nameof(slot))
		};
	}

	private static IList InvokeBuildOptions(LevelCatalog catalog, object slot, object model) {
		object result = BuildOptionsMethod.Invoke(null, new[] { catalog, slot, model })!;
		return (IList)result;
	}

	private static object NewEmptySet() {
		return NewSet(null, null, null);
	}

	private static object NewSet(int? p1, int? p2, int? p3) {
		object set = Activator.CreateInstance(CustomSetType)!;
		CustomSetType.GetField("P1Id")!.SetValue(set, p1);
		CustomSetType.GetField("P2Id")!.SetValue(set, p2);
		CustomSetType.GetField("P3Id")!.SetValue(set, p3);
		return set;
	}
}
