using sa2_hunting_teacher;
using sa2_hunting_teacher.Knuckles;
using sa2_hunting_teacher.Rouge;
using System.Reflection;

namespace unit_tests;

public class LevelCatalogTests {
	#region Get

	[Theory]
	[InlineData(Level.WildCanyon)]
	[InlineData(Level.PumpkinHill)]
	[InlineData(Level.AquaticMine)]
	[InlineData(Level.DeathChamber)]
	[InlineData(Level.MeteorHerd)]
	[InlineData(Level.DryLagoon)]
	[InlineData(Level.EggQuarters)]
	[InlineData(Level.SecurityHall)]
	[InlineData(Level.MadSpace)]
	public void Get_ReturnsPopulatedCatalog_ForEveryLevel(Level level) {
		LevelCatalog catalog = LevelCatalog.Get(level);

		Assert.NotNull(catalog);
		Assert.NotEmpty(catalog.P1);
		Assert.NotEmpty(catalog.P2);
		Assert.NotEmpty(catalog.P3);
		Assert.NotEmpty(catalog.Enemies);
		Assert.NotEmpty(catalog.Hints);
	}

	[Fact]
	public void Get_ReturnsSameInstance_OnRepeatedCalls() {
		Assert.Same(LevelCatalog.Get(Level.AquaticMine), LevelCatalog.Get(Level.AquaticMine));
	}

	[Theory]
	[InlineData(Level.WildCanyon, typeof(WildCanyon))]
	[InlineData(Level.PumpkinHill, typeof(PumpkinHill))]
	[InlineData(Level.AquaticMine, typeof(AquaticMine))]
	[InlineData(Level.DeathChamber, typeof(DeathChamber))]
	[InlineData(Level.MeteorHerd, typeof(MeteorHerd))]
	[InlineData(Level.DryLagoon, typeof(DryLagoon))]
	[InlineData(Level.EggQuarters, typeof(EggQuarters))]
	[InlineData(Level.SecurityHall, typeof(SecurityHall))]
	[InlineData(Level.MadSpace, typeof(MadSpace))]
	public void Get_ReturnsSameInstanceAsLevelClassCatalog(Level level, Type levelClass) {
		LevelCatalog onClass = (LevelCatalog)levelClass.GetProperty("Catalog", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

		Assert.Same(onClass, LevelCatalog.Get(level));
	}

	[Fact]
	public void Get_Throws_ForUnmappedLevel() {
		Assert.Throws<KeyNotFoundException>(() => LevelCatalog.Get((Level)999));
	}

	#endregion

	#region HintFor — priority order

	[Fact]
	public void HintFor_ReturnsPieceToHintEntry_WhenPresent() {
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);
		KeyValuePair<int, string> known = AquaticMine.PieceToHint.First();

		Assert.Equal(known.Value, catalog.HintFor(known.Key));
	}

	[Fact]
	public void HintFor_FallsBackToImpossiblePieces_WhenNotInPieceToHint() {
		Assert.NotEmpty(WildCanyon.ImpossiblePieces);
		LevelCatalog catalog = LevelCatalog.Get(Level.WildCanyon);
		KeyValuePair<int, string> impossible = WildCanyon.ImpossiblePieces.First();

		Assert.False(WildCanyon.PieceToHint.ContainsKey(impossible.Key));
		Assert.Equal(impossible.Value, catalog.HintFor(impossible.Key));
	}

	[Fact]
	public void HintFor_FallsBackToImpossiblePieces_ForEggQuarters() {
		Assert.NotEmpty(EggQuarters.ImpossiblePieces);
		LevelCatalog catalog = LevelCatalog.Get(Level.EggQuarters);
		KeyValuePair<int, string> impossible = EggQuarters.ImpossiblePieces.First();

		Assert.Equal(impossible.Value, catalog.HintFor(impossible.Key));
	}

	[Fact]
	public void HintFor_FallsBackToImpossiblePieces_ForSecurityHall() {
		Assert.NotEmpty(SecurityHall.ImpossiblePieces);
		LevelCatalog catalog = LevelCatalog.Get(Level.SecurityHall);
		KeyValuePair<int, string> impossible = SecurityHall.ImpossiblePieces.First();

		Assert.Equal(impossible.Value, catalog.HintFor(impossible.Key));
	}

	[Fact]
	public void HintFor_FallsBackToHexFormat_ForUnknownId() {
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);

		Assert.Equal("Piece 0xDEAD", catalog.HintFor(0xDEAD));
		Assert.Equal("Piece 0xBEEF", catalog.HintFor(0xBEEF));
	}

	[Fact]
	public void HintFor_HexFallback_UsesFourHexDigits() {
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);

		Assert.Equal("Piece 0xFFFF", catalog.HintFor(unchecked(0xFFFF)));
	}

	#endregion

	#region Hints dictionary

	[Fact]
	public void Hints_ContainsAllPieceToHintEntries() {
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);

		foreach (KeyValuePair<int, string> kvp in AquaticMine.PieceToHint) {
			Assert.True(catalog.Hints.ContainsKey(kvp.Key));
			Assert.Equal(kvp.Value, catalog.Hints[kvp.Key]);
		}
	}

	[Fact]
	public void Hints_ContainsAllImpossiblePiecesEntries_WhenLevelHasThem() {
		LevelCatalog catalog = LevelCatalog.Get(Level.WildCanyon);

		foreach (KeyValuePair<int, string> kvp in WildCanyon.ImpossiblePieces) {
			Assert.True(catalog.Hints.ContainsKey(kvp.Key));
			Assert.Equal(kvp.Value, catalog.Hints[kvp.Key]);
		}
	}

	[Fact]
	public void Hints_PieceToHintTakesPriorityOverImpossiblePieces() {
		LevelCatalog catalog = LevelCatalog.Get(Level.WildCanyon);

		foreach (KeyValuePair<int, string> kvp in WildCanyon.PieceToHint) {
			if (WildCanyon.ImpossiblePieces.ContainsKey(kvp.Key)) {
				Assert.Equal(kvp.Value, catalog.Hints[kvp.Key]);
			}
		}
	}

	#endregion

	#region Slot lists

	[Theory]
	[InlineData(Level.WildCanyon, typeof(WildCanyon))]
	[InlineData(Level.PumpkinHill, typeof(PumpkinHill))]
	[InlineData(Level.AquaticMine, typeof(AquaticMine))]
	[InlineData(Level.DeathChamber, typeof(DeathChamber))]
	[InlineData(Level.MeteorHerd, typeof(MeteorHerd))]
	[InlineData(Level.DryLagoon, typeof(DryLagoon))]
	[InlineData(Level.EggQuarters, typeof(EggQuarters))]
	[InlineData(Level.SecurityHall, typeof(SecurityHall))]
	[InlineData(Level.MadSpace, typeof(MadSpace))]
	public void SlotLists_MirrorTheirNestedEnums(Level level, Type levelClass) {
		LevelCatalog catalog = LevelCatalog.Get(level);

		Assert.Equal(EnumIds(levelClass, "P1Id").OrderBy(x => x), catalog.P1.OrderBy(x => x));
		Assert.Equal(EnumIds(levelClass, "P2Id").OrderBy(x => x), catalog.P2.OrderBy(x => x));
		Assert.Equal(EnumIds(levelClass, "P3Id").OrderBy(x => x), catalog.P3.OrderBy(x => x));
		Assert.Equal(EnumIds(levelClass, "EnemyId").OrderBy(x => x), catalog.Enemies.OrderBy(x => x));
	}

	[Theory]
	[InlineData(Level.WildCanyon)]
	[InlineData(Level.AquaticMine)]
	[InlineData(Level.MadSpace)]
	public void SlotLists_AreDisjoint(Level level) {
		LevelCatalog catalog = LevelCatalog.Get(level);

		Assert.Empty(catalog.P1.Intersect(catalog.P2));
		Assert.Empty(catalog.P1.Intersect(catalog.P3));
		Assert.Empty(catalog.P2.Intersect(catalog.P3));
		Assert.Empty(catalog.P1.Intersect(catalog.Enemies));
		Assert.Empty(catalog.P2.Intersect(catalog.Enemies));
		Assert.Empty(catalog.P3.Intersect(catalog.Enemies));
	}

	#endregion

	private static int[] EnumIds(Type levelClass, string nestedEnumName) {
		Type enumType = levelClass.GetNestedType(nestedEnumName, BindingFlags.NonPublic | BindingFlags.Public)!;
		return Enum.GetValues(enumType).Cast<object>().Select(v => Convert.ToInt32(v)).ToArray();
	}
}
