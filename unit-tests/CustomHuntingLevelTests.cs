using sa2_hunting_teacher;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace unit_tests;

public class CustomHuntingLevelTests {
	private readonly SA2Manager sa2;

	public CustomHuntingLevelTests() {
		this.sa2 = (SA2Manager)RuntimeHelpers.GetUninitializedObject(typeof(SA2Manager));
	}

	#region Constructor — Sequence materialization

	[Fact]
	public void Constructor_BuildsSequenceFromHuntingSequenceSets() {
		Dictionary<int, string> hints = new() {
			{ 0x0001, "p1-a" }, { 0x0002, "p1-b" },
			{ 0x0100, "p2-a" }, { 0x0200, "p2-b" },
			{ 0x0004, "p3-a" }, { 0x0104, "p3-b" }
		};
		HuntingSequence seq = new() {
			Id = 1,
			Name = "Test",
			Level = Level.AquaticMine,
			Sets = [
				new HuntingSet { P1Id = 0x0001, P2Id = 0x0100, P3Id = 0x0004 },
				new HuntingSet { P1Id = 0x0002, P2Id = 0x0200, P3Id = 0x0104 }
			]
		};

		CustomHuntingLevel level = new(this.sa2, 1, seq, hints);

		Set[] sequence = GetSequence(level);
		Assert.Equal(2, sequence.Length);

		Assert.Equal(0x0001, sequence[0].P1Id);
		Assert.Equal(0x0100, sequence[0].P2Id);
		Assert.Equal(0x0004, sequence[0].P3Id);
		Assert.Equal("p1-a", sequence[0].P1);
		Assert.Equal("p2-a", sequence[0].P2);
		Assert.Equal("p3-a", sequence[0].P3);

		Assert.Equal(0x0002, sequence[1].P1Id);
		Assert.Equal(0x0200, sequence[1].P2Id);
		Assert.Equal(0x0104, sequence[1].P3Id);
		Assert.Equal("p1-b", sequence[1].P1);
		Assert.Equal("p2-b", sequence[1].P2);
		Assert.Equal("p3-b", sequence[1].P3);
	}

	[Fact]
	public void Constructor_ProducesEmptySequence_WhenNoSets() {
		HuntingSequence seq = new() { Name = "empty", Level = Level.WildCanyon, Sets = new() };

		CustomHuntingLevel level = new(this.sa2, 1, seq, new Dictionary<int, string>());

		Assert.Empty(GetSequence(level));
	}

	[Fact]
	public void Constructor_PreservesSequenceOrder() {
		Dictionary<int, string> hints = new() { { 1, "a" }, { 2, "b" }, { 3, "c" } };
		HuntingSequence seq = new() {
			Name = "ordered",
			Level = Level.WildCanyon,
			Sets = [
				new HuntingSet { P1Id = 3, P2Id = 3, P3Id = 3 },
				new HuntingSet { P1Id = 1, P2Id = 1, P3Id = 1 },
				new HuntingSet { P1Id = 2, P2Id = 2, P3Id = 2 }
			]
		};

		CustomHuntingLevel level = new(this.sa2, 1, seq, hints);

		Set[] sequence = GetSequence(level);
		Assert.Equal(3, sequence[0].P1Id);
		Assert.Equal(1, sequence[1].P1Id);
		Assert.Equal(2, sequence[2].P1Id);
	}

	[Fact]
	public void Constructor_ThrowsKeyNotFound_WhenHintsMissingForId() {
		Dictionary<int, string> hints = new() { { 1, "only" } };
		HuntingSequence seq = new() {
			Name = "missing",
			Level = Level.WildCanyon,
			Sets = [new HuntingSet { P1Id = 1, P2Id = 99, P3Id = 1 }]
		};

		Assert.Throws<KeyNotFoundException>(() => new CustomHuntingLevel(this.sa2, 1, seq, hints));
	}

	#endregion

	#region LevelId

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
	public void LevelId_DerivesFromHuntingSequenceLevel(Level baseLevel, LevelId expected) {
		HuntingSequence seq = new() { Name = "x", Level = baseLevel, Sets = new() };

		CustomHuntingLevel level = new(this.sa2, 1, seq, new Dictionary<int, string>());

		Assert.Equal(expected, level.LevelId);
	}

	#endregion

	#region ToString and PieceToHintInstance

	[Fact]
	public void ToString_ReturnsHuntingSequenceName() {
		HuntingSequence seq = new() { Name = "My Custom Sequence", Level = Level.AquaticMine, Sets = new() };

		CustomHuntingLevel level = new(this.sa2, 1, seq, new Dictionary<int, string>());

		Assert.Equal("My Custom Sequence", level.ToString());
	}

	[Fact]
	public void PieceToHintInstance_ReturnsProvidedDictionaryReference() {
		Dictionary<int, string> hints = new() { { 1, "a" } };
		HuntingSequence seq = new() { Name = "x", Level = Level.AquaticMine, Sets = new() };

		CustomHuntingLevel level = new(this.sa2, 1, seq, hints);

		Assert.Same(hints, level.PieceToHintInstance);
	}

	#endregion

	private static Set[] GetSequence(HuntingLevel level) {
		PropertyInfo prop = typeof(HuntingLevel).GetProperty("Sequence", BindingFlags.Instance | BindingFlags.NonPublic)!;
		return (Set[])prop.GetValue(level)!;
	}
}
