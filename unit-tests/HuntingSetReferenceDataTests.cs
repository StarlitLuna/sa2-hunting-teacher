using Microsoft.VisualBasic.FileIO;
using sa2_hunting_teacher;
using sa2_hunting_teacher.Knuckles;
using sa2_hunting_teacher.Rouge;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace unit_tests;

public class HuntingSetReferenceDataTests {
	private static readonly string[] ExpectedCsvHeader = ["#", "Piece 1", "Piece 2", "Piece 3"];
	private static readonly string[] PieceColumnNames = ["Piece 1", "Piece 2", "Piece 3"];
	private const int MaxFailureListItems = 25;

	public static IEnumerable<object[]> ReferenceCases {
		get {
			yield return [
				new ReferenceCase(
					"Wild Canyon",
					((int)LevelId.WildCanyon).ToString(CultureInfo.InvariantCulture),
					Level.WildCanyon,
					Path.Combine("SetData", "KnucklesSets.json"),
					Path.Combine("TestData", "Knuckles", "WildCanyon.csv"),
					WildCanyon.PieceToHint,
					1024,
					new Dictionary<HintOverrideKey, int>()
				)
			];

			yield return [
				new ReferenceCase(
					"Pumpkin Hill",
					((int)LevelId.PumpkinHill).ToString(CultureInfo.InvariantCulture),
					Level.PumpkinHill,
					Path.Combine("SetData", "KnucklesSets.json"),
					Path.Combine("TestData", "Knuckles", "PumpkinHill.csv"),
					PumpkinHill.PieceToHint,
					1024,
					PumpkinHillHintOverrides()
				)
			];

			yield return [
				new ReferenceCase(
					"Pumpkin Hill Story",
					((int)LevelId.PumpkinHill).ToString(CultureInfo.InvariantCulture) + "-story",
					Level.PumpkinHill,
					Path.Combine("SetData", "KnucklesSets.json"),
					Path.Combine("TestData", "Knuckles", "PumpkinHillStory.csv"),
					PumpkinHill.PieceToHint,
					1024,
					PumpkinHillHintOverrides()
				)
			];

			yield return [
				new ReferenceCase(
					"Aquatic Mine",
					((int)LevelId.AquaticMine).ToString(CultureInfo.InvariantCulture),
					Level.AquaticMine,
					Path.Combine("SetData", "KnucklesSets.json"),
					Path.Combine("TestData", "Knuckles", "AquaticMine.csv"),
					AquaticMine.PieceToHint,
					1024,
					new Dictionary<HintOverrideKey, int>()
				)
			];

			yield return [
				new ReferenceCase(
					"Death Chamber",
					((int)LevelId.DeathChamber).ToString(CultureInfo.InvariantCulture),
					Level.DeathChamber,
					Path.Combine("SetData", "KnucklesSets.json"),
					Path.Combine("TestData", "Knuckles", "DeathChamber.csv"),
					DeathChamber.PieceToHint,
					1024,
					new Dictionary<HintOverrideKey, int>()
				)
			];

			yield return [
				new ReferenceCase(
					"Meteor Herd",
					((int)LevelId.MeteorHerd).ToString(CultureInfo.InvariantCulture),
					Level.MeteorHerd,
					Path.Combine("SetData", "KnucklesSets.json"),
					Path.Combine("TestData", "Knuckles", "MeteorHerd.csv"),
					MeteorHerd.PieceToHint,
					1024,
					new Dictionary<HintOverrideKey, int>()
				)
			];

			yield return [
				new ReferenceCase(
					"Security Hall",
					((int)LevelId.SecurityHall).ToString(CultureInfo.InvariantCulture),
					Level.SecurityHall,
					Path.Combine("SetData", "RougeSets.json"),
					Path.Combine("TestData", "Rouge", "SecurityHall.csv"),
					SecurityHall.PieceToHint,
					1024,
					new Dictionary<HintOverrideKey, int>()
				)
			];

			yield return [
				new ReferenceCase(
					"Dry Lagoon",
					((int)LevelId.DryLagoon).ToString(CultureInfo.InvariantCulture),
					Level.DryLagoon,
					Path.Combine("SetData", "RougeSets.json"),
					Path.Combine("TestData", "Rouge", "DryLagoon.csv"),
					DryLagoon.PieceToHint,
					1024,
					new Dictionary<HintOverrideKey, int>()
				)
			];

			yield return [
				new ReferenceCase(
					"Egg Quarters",
					((int)LevelId.EggQuarters).ToString(CultureInfo.InvariantCulture),
					Level.EggQuarters,
					Path.Combine("SetData", "RougeSets.json"),
					Path.Combine("TestData", "Rouge", "EggQuarters.csv"),
					EggQuarters.PieceToHint,
					1024,
					new Dictionary<HintOverrideKey, int>()
				)
			];

			yield return [
				new ReferenceCase(
					"Egg Quarters Story",
					((int)LevelId.EggQuarters).ToString(CultureInfo.InvariantCulture) + "-story",
					Level.EggQuarters,
					Path.Combine("SetData", "RougeSets.json"),
					Path.Combine("TestData", "Rouge", "EggQuartersStory.csv"),
					EggQuarters.PieceToHint,
					1024,
					new Dictionary<HintOverrideKey, int>()
				)
			];

			yield return [
				new ReferenceCase(
					"Mad Space",
					((int)LevelId.MadSpace).ToString(CultureInfo.InvariantCulture),
					Level.MadSpace,
					Path.Combine("SetData", "RougeSets.json"),
					Path.Combine("TestData", "Rouge", "MadSpace.csv"),
					MadSpace.PieceToHint,
					1024,
					new Dictionary<HintOverrideKey, int>(),
					HintTransform.MadSpaceReverse,
					StringComparison.OrdinalIgnoreCase
				)
			];
		}
	}

	private static Dictionary<HintOverrideKey, int> PumpkinHillHintOverrides() {
		return new Dictionary<HintOverrideKey, int> {
			{ new HintOverrideKey(0, "King of the hill."), 0x000A },
			{ new HintOverrideKey(1, "King of the hill."), 0x000A },
			{ new HintOverrideKey(2, "King of the hill."), 0x0004 }
		};
	}

	[Theory]
	[MemberData(nameof(ReferenceCases))]
	public void SetData_MatchesReferenceCsv(ReferenceCase referenceCase) {
		string csvPath = Path.Combine(AppContext.BaseDirectory, referenceCase.CsvPath);
		string jsonPath = Path.Combine(AppContext.BaseDirectory, referenceCase.SetsJsonPath);

		Dictionary<string, int[]> expectedSets = ReadExpectedSets(referenceCase, csvPath);
		Dictionary<string, Dictionary<string, int[]>> actualSetsByLevel = ReadSetJson(jsonPath);

		AssertActualSetsMatchExpected(referenceCase, expectedSets, actualSetsByLevel);
	}

	private static Dictionary<string, int[]> ReadExpectedSets(ReferenceCase referenceCase, string csvPath) {
		LevelCatalog catalog = LevelCatalog.Get(referenceCase.Level);
		HashSet<int>[] allowedIdsByColumn = [
			new HashSet<int>(catalog.P1.Concat(catalog.Enemies)),
			new HashSet<int>(catalog.P2.Concat(catalog.Enemies)),
			new HashSet<int>(catalog.P3.Concat(catalog.Enemies))
		];

		Dictionary<string, int[]> expectedSets = new();

		using TextFieldParser parser = new(csvPath);
		parser.TextFieldType = FieldType.Delimited;
		parser.SetDelimiters(",");
		parser.HasFieldsEnclosedInQuotes = true;

		string[] header = parser.ReadFields() ?? throw new InvalidOperationException($"{csvPath} is empty.");
		Assert.Equal(ExpectedCsvHeader, header);

		int expectedSetId = 0;
		while (!parser.EndOfData) {
			string[] fields = parser.ReadFields() ?? throw new InvalidOperationException(
				$"{referenceCase.Name}: CSV row for set {expectedSetId} could not be read."
			);

			Assert.Equal(4, fields.Length);

			int setId = int.Parse(fields[0], CultureInfo.InvariantCulture);
			Assert.Equal(expectedSetId, setId);

			expectedSets.Add(
				setId.ToString(CultureInfo.InvariantCulture),
				[
					ResolveHint(referenceCase, setId, 0, ApplyHintTransform(referenceCase, fields[1]), allowedIdsByColumn[0]),
					ResolveHint(referenceCase, setId, 1, ApplyHintTransform(referenceCase, fields[2]), allowedIdsByColumn[1]),
					ResolveHint(referenceCase, setId, 2, ApplyHintTransform(referenceCase, fields[3]), allowedIdsByColumn[2])
				]
			);

			expectedSetId++;
		}

		Assert.Equal(referenceCase.ExpectedSetCount, expectedSetId);
		return expectedSets;
	}

	private static int ResolveHint(
		ReferenceCase referenceCase,
		int setId,
		int columnIndex,
		string csvHint,
		HashSet<int> allowedIds
	) {
		if (referenceCase.HintOverrides.TryGetValue(new HintOverrideKey(columnIndex, csvHint), out int overrideId)) {
			Assert.True(
				allowedIds.Contains(overrideId),
				$"{referenceCase.Name}: set {setId} {PieceColumnNames[columnIndex]} override " +
				$"{overrideId} (0x{overrideId:X4}) is not valid for this column."
			);
			Assert.True(
				referenceCase.PieceToHint.TryGetValue(overrideId, out string? overrideHint) &&
					string.Equals(overrideHint, csvHint, referenceCase.HintComparison),
				$"{referenceCase.Name}: set {setId} {PieceColumnNames[columnIndex]} override " +
				$"{overrideId} (0x{overrideId:X4}) does not match hint '{csvHint}'."
			);

			return overrideId;
		}

		List<int> matches = allowedIds
			.Where(id => referenceCase.PieceToHint.TryGetValue(id, out string? hint) &&
				string.Equals(hint, csvHint, referenceCase.HintComparison))
			.Distinct()
			.Order()
			.ToList();

		if (matches.Count == 1) {
			return matches[0];
		}

		string columnName = PieceColumnNames[columnIndex];
		if (matches.Count == 0) {
			List<int> allDictionaryMatches = referenceCase.PieceToHint
				.Where(kvp => string.Equals(kvp.Value, csvHint, referenceCase.HintComparison))
				.Select(kvp => kvp.Key)
				.Order()
				.ToList();

			Assert.Fail(
				$"{referenceCase.Name}: set {setId} {columnName} hint '{csvHint}' did not resolve to an allowed ID. " +
				$"Dictionary matches: {FormatIdsOrNone(allDictionaryMatches)}."
			);
		}

		Assert.Fail(
			$"{referenceCase.Name}: set {setId} {columnName} hint '{csvHint}' resolved to multiple allowed IDs: " +
			$"{FormatIdsOrNone(matches)}."
		);

		throw new UnreachableException();
	}

	private static string ApplyHintTransform(ReferenceCase referenceCase, string csvHint) {
		return referenceCase.HintTransform switch {
			HintTransform.None => csvHint,
			HintTransform.MadSpaceReverse => ReverseMadSpaceHint(csvHint),
			_ => throw new UnreachableException()
		};
	}

	private static string ReverseMadSpaceHint(string csvHint) {
		int closeParenthesis = csvHint.LastIndexOf(')');
		if (closeParenthesis != csvHint.Length - 1) {
			return ReverseString(csvHint).Trim();
		}

		int openParenthesis = csvHint.LastIndexOf('(');
		if (openParenthesis < 0) {
			return ReverseString(csvHint).Trim();
		}

		string beforeParentheses = csvHint[..openParenthesis].TrimEnd();
		string parentheticalText = csvHint[(openParenthesis + 1)..closeParenthesis];
		return $"{ReverseString(beforeParentheses).Trim()} ({ReverseString(parentheticalText).Trim()})";
	}

	private static string ReverseString(string value) {
		char[] chars = value.ToCharArray();
		Array.Reverse(chars);
		return new string(chars);
	}

	private static Dictionary<string, Dictionary<string, int[]>> ReadSetJson(string jsonPath) {
		string json = File.ReadAllText(jsonPath);
		Dictionary<string, Dictionary<string, int[]>>? result =
			JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int[]>>>(json);

		return result ?? throw new InvalidOperationException($"{jsonPath} did not contain set data.");
	}

	private static void AssertActualSetsMatchExpected(
		ReferenceCase referenceCase,
		Dictionary<string, int[]> expectedSets,
		Dictionary<string, Dictionary<string, int[]>> actualSetsByLevel
	) {
		string levelKey = referenceCase.LevelKey;
		Assert.True(
			actualSetsByLevel.TryGetValue(levelKey, out Dictionary<string, int[]>? actualSets),
			$"{referenceCase.SetsJsonPath} does not contain level {levelKey} for {referenceCase.Name}."
		);

		string[] missingSetIds = expectedSets.Keys
			.Except(actualSets.Keys)
			.OrderBy(NumericKey)
			.ToArray();
		string[] extraSetIds = actualSets.Keys
			.Except(expectedSets.Keys)
			.OrderBy(NumericKey)
			.ToArray();

		Assert.True(
			missingSetIds.Length == 0,
			$"{referenceCase.Name}: missing set IDs in {referenceCase.SetsJsonPath}: {FormatKeys(missingSetIds)}."
		);
		Assert.True(
			extraSetIds.Length == 0,
			$"{referenceCase.Name}: extra set IDs in {referenceCase.SetsJsonPath}: {FormatKeys(extraSetIds)}."
		);

		foreach (KeyValuePair<string, int[]> expectedSet in expectedSets.OrderBy(kvp => NumericKey(kvp.Key))) {
			int[] actualSet = actualSets[expectedSet.Key];
			Assert.True(
				expectedSet.Value.SequenceEqual(actualSet),
				$"{referenceCase.Name}: set {expectedSet.Key} expected {FormatIds(expectedSet.Value)} " +
				$"but found {FormatIds(actualSet)}."
			);
		}
	}

	private static int NumericKey(string key) {
		return int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out int value)
			? value
			: int.MaxValue;
	}

	private static string FormatIdsOrNone(IEnumerable<int> ids) {
		int[] idArray = ids.ToArray();
		return idArray.Length == 0 ? "none" : FormatIds(idArray);
	}

	private static string FormatIds(IEnumerable<int> ids) {
		return $"[{string.Join(", ", ids.Select(id => $"{id} (0x{id:X4})"))}]";
	}

	private static string FormatKeys(string[] keys) {
		if (keys.Length <= MaxFailureListItems) {
			return string.Join(", ", keys);
		}

		return $"{string.Join(", ", keys.Take(MaxFailureListItems))}, ... ({keys.Length} total)";
	}

	public sealed record ReferenceCase(
		string Name,
		string LevelKey,
		Level Level,
		string SetsJsonPath,
		string CsvPath,
		IReadOnlyDictionary<int, string> PieceToHint,
		int ExpectedSetCount,
		IReadOnlyDictionary<HintOverrideKey, int> HintOverrides,
		HintTransform HintTransform = HintTransform.None,
		StringComparison HintComparison = StringComparison.Ordinal
	);

	public sealed record HintOverrideKey(int ColumnIndex, string Hint);

	public enum HintTransform {
		None,
		MadSpaceReverse
	}
}
