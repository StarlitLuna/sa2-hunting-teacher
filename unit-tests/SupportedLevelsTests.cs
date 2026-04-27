using sa2_hunting_teacher;
using System.Reflection;

namespace unit_tests;

public class SupportedLevelsTests {
	private static readonly Type SupportedLevelsType =
		typeof(LevelRow).Assembly.GetType("sa2_hunting_teacher.SupportedLevels", throwOnError: true)!;

	#region LevelToLevelId

	[Theory]
	[InlineData(Level.WildCanyon, LevelId.WildCanyon)]
	[InlineData(Level.PumpkinHill, LevelId.PumpkinHill)]
	[InlineData(Level.AquaticMine, LevelId.AquaticMine)]
	[InlineData(Level.DeathChamber, LevelId.DeathChamber)]
	[InlineData(Level.MeteorHerd, LevelId.MeteorHerd)]
	[InlineData(Level.DryLagoon, LevelId.DryLagoon)]
	[InlineData(Level.EggQuarters, LevelId.EggQuarters)]
	[InlineData(Level.SecurityHall, LevelId.SecurityHall)]
	[InlineData(Level.MadSpace, LevelId.MadSpace)]
	public void LevelToLevelId_MapsEveryUiLevelToHelperLevelId(Level uiLevel, LevelId expected) {
		Dictionary<Level, LevelId> map = GetLevelToLevelId();

		Assert.Equal(expected, map[uiLevel]);
	}

	[Fact]
	public void LevelToLevelId_CoversEveryDeclaredLevel() {
		Dictionary<Level, LevelId> map = GetLevelToLevelId();

		foreach (Level level in Enum.GetValues<Level>()) {
			Assert.True(map.ContainsKey(level), $"LevelToLevelId is missing entry for {level}");
		}
	}

	[Fact]
	public void LevelToLevelId_HasNoExtraEntries() {
		Dictionary<Level, LevelId> map = GetLevelToLevelId();

		Assert.Equal(Enum.GetValues<Level>().Length, map.Count);
	}

	#endregion

	#region Map / categories

	[Fact]
	public void Map_CoversEveryDeclaredLevel() {
		Dictionary<Level, (string LevelText, string Category)> map = GetMap();

		foreach (Level level in Enum.GetValues<Level>()) {
			Assert.True(map.ContainsKey(level), $"Map is missing entry for {level}");
		}
	}

	[Fact]
	public void Map_CategorizesEveryLevelAsKnucklesOrRouge() {
		Dictionary<Level, (string LevelText, string Category)> map = GetMap();

		foreach (KeyValuePair<Level, (string LevelText, string Category)> kvp in map) {
			Assert.True(
				kvp.Value.Category is "Knuckles" or "Rouge",
				$"Level {kvp.Key} has unexpected category {kvp.Value.Category}"
			);
		}
	}

	[Fact]
	public void Map_HasNonEmptyDisplayText_ForEveryLevel() {
		Dictionary<Level, (string LevelText, string Category)> map = GetMap();

		foreach (KeyValuePair<Level, (string LevelText, string Category)> kvp in map) {
			Assert.False(string.IsNullOrWhiteSpace(kvp.Value.LevelText), $"Level {kvp.Key} has empty display text");
		}
	}

	#endregion

	#region CustomGroup constant

	[Fact]
	public void CustomGroup_IsCustom() {
		FieldInfo field = SupportedLevelsType.GetField("CustomGroup", BindingFlags.Public | BindingFlags.Static)!;

		Assert.Equal("Custom", field.GetValue(null));
	}

	#endregion

	private static Dictionary<Level, LevelId> GetLevelToLevelId() {
		FieldInfo field = SupportedLevelsType.GetField("LevelToLevelId", BindingFlags.Public | BindingFlags.Static)!;
		return (Dictionary<Level, LevelId>)field.GetValue(null)!;
	}

	private static Dictionary<Level, (string LevelText, string Category)> GetMap() {
		FieldInfo field = SupportedLevelsType.GetField("Map", BindingFlags.Public | BindingFlags.Static)!;
		return (Dictionary<Level, (string LevelText, string Category)>)field.GetValue(null)!;
	}
}
