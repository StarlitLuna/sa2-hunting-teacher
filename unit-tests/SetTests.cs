using sa2_hunting_teacher;

namespace unit_tests;

public class SetTests {
	#region Constructor

	[Fact]
	public void Constructor_AssignsAllSixFields() {
		Set set = new(0x1234, 0x5678, 0x9ABC, "first", "second", "third");

		Assert.Equal(0x1234, set.P1Id);
		Assert.Equal(0x5678, set.P2Id);
		Assert.Equal(0x9ABC, set.P3Id);
		Assert.Equal("first", set.P1);
		Assert.Equal("second", set.P2);
		Assert.Equal("third", set.P3);
	}

	[Fact]
	public void Constructor_AcceptsEmptyHints() {
		Set set = new(1, 2, 3, "", "", "");

		Assert.Equal("", set.P1);
		Assert.Equal("", set.P2);
		Assert.Equal("", set.P3);
	}

	[Fact]
	public void Constructor_AcceptsNegativeIds() {
		Set set = new(-1, -2, -3, "a", "b", "c");

		Assert.Equal(-1, set.P1Id);
		Assert.Equal(-2, set.P2Id);
		Assert.Equal(-3, set.P3Id);
	}

	[Fact]
	public void Fields_AreMutable() {
		Set set = new(1, 2, 3, "a", "b", "c");

		set.P1Id = 99;
		set.P1 = "x";

		Assert.Equal(99, set.P1Id);
		Assert.Equal("x", set.P1);
	}

	#endregion

	#region ToString

	[Fact]
	public void ToString_FormatsHintsAcrossThreeTabbedLines() {
		Set set = new(0, 0, 0, "alpha", "beta", "gamma");

		string expected = $"{Environment.NewLine}\tP1: alpha{Environment.NewLine}\tP2: beta{Environment.NewLine}\tP3: gamma";

		Assert.Equal(expected, set.ToString());
	}

	[Fact]
	public void ToString_IgnoresIdValues() {
		Set a = new(1, 2, 3, "x", "y", "z");
		Set b = new(99, 98, 97, "x", "y", "z");

		Assert.Equal(a.ToString(), b.ToString());
	}

	[Fact]
	public void ToString_IncludesLiteralEmptyHints() {
		Set set = new(0, 0, 0, "", "", "");

		string expected = $"{Environment.NewLine}\tP1: {Environment.NewLine}\tP2: {Environment.NewLine}\tP3: ";

		Assert.Equal(expected, set.ToString());
	}

	#endregion

	#region EnumKey

	[Theory]
	[InlineData(LevelId.BasicTest)]
	[InlineData(LevelId.WildCanyon)]
	[InlineData(LevelId.MadSpace)]
	[InlineData(LevelId.Route101280)]
	[InlineData(LevelId.ChaoWorld)]
	[InlineData(LevelId.Invalid)]
	public void EnumKey_ConvertsIntEnumToUnderlyingValue(LevelId value) {
		Assert.Equal((int)value, Set.EnumKey(value));
	}

	[Fact]
	public void EnumKey_ConvertsByteEnumToInt() {
		Assert.Equal(0, Set.EnumKey(TestByteEnum.Zero));
		Assert.Equal(1, Set.EnumKey(TestByteEnum.One));
		Assert.Equal(255, Set.EnumKey(TestByteEnum.Max));
	}

	[Fact]
	public void EnumKey_ConvertsShortEnumToInt() {
		Assert.Equal(0x0A0A, Set.EnumKey(TestShortEnum.LargeWord));
	}

	[Fact]
	public void EnumKey_ConvertsLongEnumWithinIntRangeToInt() {
		Assert.Equal(42, Set.EnumKey(TestLongEnum.FortyTwo));
	}

	#endregion

	#region Create (int overload)

	[Fact]
	public void Create_FromInts_PopulatesIdsAndLooksUpHints() {
		Dictionary<int, string> dict = new() {
			{ 1, "first" },
			{ 2, "second" },
			{ 3, "third" },
		};

		Set set = Set.Create(1, 2, 3, dict);

		Assert.Equal(1, set.P1Id);
		Assert.Equal(2, set.P2Id);
		Assert.Equal(3, set.P3Id);
		Assert.Equal("first", set.P1);
		Assert.Equal("second", set.P2);
		Assert.Equal("third", set.P3);
	}

	[Fact]
	public void Create_FromInts_AllowsRepeatedIds() {
		Dictionary<int, string> dict = new() { { 7, "lucky" } };

		Set set = Set.Create(7, 7, 7, dict);

		Assert.Equal(7, set.P1Id);
		Assert.Equal(7, set.P2Id);
		Assert.Equal(7, set.P3Id);
		Assert.Equal("lucky", set.P1);
		Assert.Equal("lucky", set.P2);
		Assert.Equal("lucky", set.P3);
	}

	[Fact]
	public void Create_FromInts_ThrowsKeyNotFoundForMissingKey() {
		Dictionary<int, string> dict = new() { { 1, "first" } };

		Assert.Throws<KeyNotFoundException>(() => Set.Create(1, 99, 1, dict));
	}

	#endregion

	#region Create (generic enum overload)

	[Fact]
	public void Create_FromEnums_ConvertsAndLooksUpHints() {
		Dictionary<int, string> dict = new() {
			{ (int)LevelId.WildCanyon, "wc" },
			{ (int)LevelId.PumpkinHill, "ph" },
			{ (int)LevelId.AquaticMine, "am" },
		};

		Set set = Set.Create(LevelId.WildCanyon, LevelId.PumpkinHill, LevelId.AquaticMine, dict);

		Assert.Equal((int)LevelId.WildCanyon, set.P1Id);
		Assert.Equal((int)LevelId.PumpkinHill, set.P2Id);
		Assert.Equal((int)LevelId.AquaticMine, set.P3Id);
		Assert.Equal("wc", set.P1);
		Assert.Equal("ph", set.P2);
		Assert.Equal("am", set.P3);
	}

	[Fact]
	public void Create_FromEnums_AllowsHeterogeneousEnumTypes() {
		Dictionary<int, string> dict = new() {
			{ 1, "byte-one" },
			{ 0x0A0A, "short-word" },
			{ (int)LevelId.WildCanyon, "wc" },
		};

		Set set = Set.Create(TestByteEnum.One, TestShortEnum.LargeWord, LevelId.WildCanyon, dict);

		Assert.Equal(1, set.P1Id);
		Assert.Equal(0x0A0A, set.P2Id);
		Assert.Equal((int)LevelId.WildCanyon, set.P3Id);
		Assert.Equal("byte-one", set.P1);
		Assert.Equal("short-word", set.P2);
		Assert.Equal("wc", set.P3);
	}

	[Fact]
	public void Create_FromEnums_ThrowsKeyNotFoundForMissingValue() {
		Dictionary<int, string> dict = new() { { (int)LevelId.WildCanyon, "wc" } };

		Assert.Throws<KeyNotFoundException>(
			() => Set.Create(LevelId.WildCanyon, LevelId.PumpkinHill, LevelId.WildCanyon, dict)
		);
	}

	#endregion

	private enum TestByteEnum : byte {
		Zero = 0,
		One = 1,
		Max = 255,
	}

	private enum TestShortEnum : short {
		LargeWord = 0x0A0A,
	}

	private enum TestLongEnum : long {
		FortyTwo = 42,
	}
}
