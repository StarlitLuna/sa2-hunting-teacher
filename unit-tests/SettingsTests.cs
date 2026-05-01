using sa2_hunting_teacher;
using System.Reflection;
using System.Text.Json;

namespace unit_tests;

[Collection(StaticStateCollection.Name)]
public class SettingsTests : IDisposable {
	private readonly bool appDataDirExisted;
	private readonly bool settingsFileExisted;
	private readonly byte[]? backupContent;

	public SettingsTests() {
		this.appDataDirExisted = Directory.Exists(Settings.AppDataPath);
		this.settingsFileExisted = File.Exists(Settings.SettingsPath);
		if (this.settingsFileExisted) {
			this.backupContent = File.ReadAllBytes(Settings.SettingsPath);
		}
	}

	public void Dispose() {
		if (!Directory.Exists(Settings.AppDataPath)) {
			Directory.CreateDirectory(Settings.AppDataPath);
		}

		if (this.settingsFileExisted) {
			File.WriteAllBytes(Settings.SettingsPath, this.backupContent!);
		} else if (File.Exists(Settings.SettingsPath)) {
			File.Delete(Settings.SettingsPath);
		}

		if (!this.appDataDirExisted) {
			try {
				Directory.Delete(Settings.AppDataPath, recursive: false);
			} catch {
			}
		}

		GC.SuppressFinalize(this);
	}

	#region Defaults / constructor

	[Fact]
	public void Constructor_HasDocumentedDefaultValues() {
		Settings settings = NewSettings();

		Assert.Equal(MspHints.ALTERNATING, settings.MspHints);
		Assert.False(settings.BackToMenu);
		Assert.True(settings.TimerReset);
		Assert.False(settings.RepetitionsInPlace);
		Assert.Equal(3, settings.Repetitions);
		Assert.NotNull(settings.CustomSequences);
		Assert.Empty(settings.CustomSequences);
		Assert.Equal(1L, settings.NextSequenceId);
	}

	[Fact]
	public void Constructor_IsMarkedObsolete() {
		ConstructorInfo? ctor = typeof(Settings).GetConstructor(Type.EmptyTypes);

		Assert.NotNull(ctor);
		Assert.NotNull(ctor!.GetCustomAttribute<ObsoleteAttribute>());
	}

	#endregion

	#region Path constants

	[Fact]
	public void AppDataPath_IsBeneathLocalApplicationData() {
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		Assert.StartsWith(localAppData, Settings.AppDataPath);
		Assert.EndsWith("SA2 Hunting Teacher", Settings.AppDataPath);
	}

	[Fact]
	public void SettingsPath_IsSettingsJsonInsideAppDataPath() {
		Assert.Equal(Path.Combine(Settings.AppDataPath, "settings.json"), Settings.SettingsPath);
	}

	#endregion

	#region Save

	[Fact]
	public void Save_WritesJsonToSettingsPath() {
		Settings settings = NewSettings();
		settings.MspHints = MspHints.FIXED;
		settings.Repetitions = 7;

		settings.Save();

		Assert.True(File.Exists(Settings.SettingsPath));
		string json = File.ReadAllText(Settings.SettingsPath);
		Assert.Contains("\"MspHints\": 1", json);
		Assert.Contains("\"Repetitions\": 7", json);
	}

	[Fact]
	public void Save_WritesIndentedJson() {
		Settings settings = NewSettings();

		settings.Save();

		string json = File.ReadAllText(Settings.SettingsPath);
		Assert.Contains(Environment.NewLine, json);
		Assert.Contains("  ", json);
	}

	[Fact]
	public void Save_OverwritesExistingFile() {
		Settings first = NewSettings();
		first.Repetitions = 5;
		first.Save();

		Settings second = NewSettings();
		second.Repetitions = 9;
		second.Save();

		string json = File.ReadAllText(Settings.SettingsPath);
		Assert.Contains("\"Repetitions\": 9", json);
		Assert.DoesNotContain("\"Repetitions\": 5", json);
	}

	[Fact]
	public void Save_SerializesAllProperties() {
		Settings settings = NewSettings();
		settings.MspHints = MspHints.ALTERNATING_REVERSED;
		settings.BackToMenu = true;
		settings.TimerReset = false;
		settings.RepetitionsInPlace = true;
		settings.Repetitions = 11;
		settings.NextSequenceId = 42;
		settings.CustomSequences.Add(new HuntingSequence {
			Id = 7,
			Name = "Test",
			Level = Level.WildCanyon,
			Sets = [new HuntingSet { P1Id = 1, P2Id = 2, P3Id = 3 }],
		});

		settings.Save();

		string json = File.ReadAllText(Settings.SettingsPath);
		Assert.Contains("\"MspHints\": 3", json);
		Assert.Contains("\"BackToMenu\": true", json);
		Assert.Contains("\"TimerReset\": false", json);
		Assert.Contains("\"RepetitionsInPlace\": true", json);
		Assert.Contains("\"Repetitions\": 11", json);
		Assert.Contains("\"CustomSequences\":", json);
		Assert.Contains("\"Id\": 7", json);
		Assert.Contains("\"Name\": \"Test\"", json);
		Assert.Contains("\"NextSequenceId\": 42", json);
	}

	#endregion

	#region Load

	[Fact]
	public void Load_ReturnsDefaults_WhenSettingsFileDoesNotExist() {
		if (!Directory.Exists(Settings.AppDataPath)) {
			Directory.CreateDirectory(Settings.AppDataPath);
		}
		if (File.Exists(Settings.SettingsPath)) {
			File.Delete(Settings.SettingsPath);
		}

		Settings result = Settings.Load();

		AssertHasDefaults(result);
	}

	[Fact]
	public void Load_CreatesAppDataDirectory_AndReturnsDefaults_WhenDirectoryDoesNotExist() {
		if (Directory.Exists(Settings.AppDataPath)) {
			string[] entries = Directory.GetFileSystemEntries(Settings.AppDataPath);
			bool onlyHasSettingsFile = entries.Length == 0
				|| (entries.Length == 1 && string.Equals(Path.GetFileName(entries[0]), "settings.json", StringComparison.OrdinalIgnoreCase));
			if (!onlyHasSettingsFile) {
				return;
			}
			if (File.Exists(Settings.SettingsPath)) {
				File.Delete(Settings.SettingsPath);
			}
			Directory.Delete(Settings.AppDataPath);
		}

		Settings result = Settings.Load();

		Assert.True(Directory.Exists(Settings.AppDataPath));
		AssertHasDefaults(result);
	}

	[Fact]
	public void Load_DeserializesAllProperties_FromExistingFile() {
		EnsureAppDataDir();
		string json = """
			{
				"MspHints": 0,
				"BackToMenu": true,
				"TimerReset": false,
				"RepetitionsInPlace": true,
				"Repetitions": 13,
				"NextSequenceId": 99,
				"CustomSequences": [
					{
						"Id": 4,
						"Name": "Loaded",
						"Level": 0,
						"Sets": [
							{ "P1Id": 10, "P2Id": 20, "P3Id": 30 }
						]
					}
				]
			}
			""";
		File.WriteAllText(Settings.SettingsPath, json);

		Settings result = Settings.Load();

		Assert.Equal(MspHints.REVERSED, result.MspHints);
		Assert.True(result.BackToMenu);
		Assert.False(result.TimerReset);
		Assert.True(result.RepetitionsInPlace);
		Assert.Equal(13, result.Repetitions);
		Assert.Equal(99L, result.NextSequenceId);
		Assert.Single(result.CustomSequences);
		Assert.Equal(4L, result.CustomSequences[0].Id);
		Assert.Equal("Loaded", result.CustomSequences[0].Name);
		Assert.Equal(Level.WildCanyon, result.CustomSequences[0].Level);
		Assert.Single(result.CustomSequences[0].Sets);
		Assert.Equal(10, result.CustomSequences[0].Sets[0].P1Id);
		Assert.Equal(20, result.CustomSequences[0].Sets[0].P2Id);
		Assert.Equal(30, result.CustomSequences[0].Sets[0].P3Id);
	}

	[Fact]
	public void Load_ReturnsNewSettings_WhenJsonIsNullLiteral() {
		EnsureAppDataDir();
		File.WriteAllText(Settings.SettingsPath, "null");

		Settings result = Settings.Load();

		AssertHasDefaults(result);
	}

	[Fact]
	public void Load_AllowsTrailingCommas() {
		EnsureAppDataDir();
		File.WriteAllText(Settings.SettingsPath, """
			{
				"Repetitions": 4,
				"BackToMenu": true,
			}
			""");

		Settings result = Settings.Load();

		Assert.Equal(4, result.Repetitions);
		Assert.True(result.BackToMenu);
	}

	[Fact]
	public void Load_SkipsComments() {
		EnsureAppDataDir();
		File.WriteAllText(Settings.SettingsPath, """
			{
				// line comment
				"Repetitions": 6,
				/* block comment */
				"TimerReset": false
			}
			""");

		Settings result = Settings.Load();

		Assert.Equal(6, result.Repetitions);
		Assert.False(result.TimerReset);
	}

	[Fact]
	public void Load_IsCaseSensitive_ForPropertyNames() {
		EnsureAppDataDir();
		File.WriteAllText(Settings.SettingsPath, """
			{
				"msphints": 0,
				"timerreset": false,
				"repetitions": 99
			}
			""");

		Settings result = Settings.Load();

		Assert.Equal(MspHints.ALTERNATING, result.MspHints);
		Assert.True(result.TimerReset);
		Assert.Equal(3, result.Repetitions);
	}

	[Fact]
	public void Load_Throws_WhenFileContainsInvalidJson() {
		EnsureAppDataDir();
		File.WriteAllText(Settings.SettingsPath, "{ this is not json");

		Assert.Throws<JsonException>(() => Settings.Load());
	}

	#endregion

	#region Round trip

	[Fact]
	public void RoundTrip_SaveThenLoad_PreservesAllProperties() {
		Settings original = NewSettings();
		original.MspHints = MspHints.ALTERNATING_REVERSED;
		original.BackToMenu = true;
		original.TimerReset = false;
		original.RepetitionsInPlace = true;
		original.Repetitions = 17;
		original.NextSequenceId = 1234567890123L;
		original.CustomSequences.Add(new HuntingSequence {
			Id = 1234567890122L,
			Name = "Round Trip",
			Level = Level.MadSpace,
			Sets = [
				new HuntingSet { P1Id = 0x1234, P2Id = 0x5678, P3Id = 0x9ABC },
				new HuntingSet { P1Id = 1, P2Id = 2, P3Id = 3 },
			],
		});

		original.Save();
		Settings loaded = Settings.Load();

		Assert.Equal(original.MspHints, loaded.MspHints);
		Assert.Equal(original.BackToMenu, loaded.BackToMenu);
		Assert.Equal(original.TimerReset, loaded.TimerReset);
		Assert.Equal(original.RepetitionsInPlace, loaded.RepetitionsInPlace);
		Assert.Equal(original.Repetitions, loaded.Repetitions);
		Assert.Equal(original.NextSequenceId, loaded.NextSequenceId);
		Assert.Equal(original.CustomSequences.Count, loaded.CustomSequences.Count);
		Assert.Equal(original.CustomSequences[0].Id, loaded.CustomSequences[0].Id);
		Assert.Equal(original.CustomSequences[0].Name, loaded.CustomSequences[0].Name);
		Assert.Equal(original.CustomSequences[0].Level, loaded.CustomSequences[0].Level);
		Assert.Equal(original.CustomSequences[0].Sets.Count, loaded.CustomSequences[0].Sets.Count);
		Assert.Equal(0x1234, loaded.CustomSequences[0].Sets[0].P1Id);
		Assert.Equal(0x5678, loaded.CustomSequences[0].Sets[0].P2Id);
		Assert.Equal(0x9ABC, loaded.CustomSequences[0].Sets[0].P3Id);
	}

	[Theory]
	[InlineData(MspHints.REVERSED)]
	[InlineData(MspHints.FIXED)]
	[InlineData(MspHints.ALTERNATING)]
	[InlineData(MspHints.ALTERNATING_REVERSED)]
	public void RoundTrip_PreservesEachMspHintsValue(MspHints value) {
		Settings original = NewSettings();
		original.MspHints = value;

		original.Save();
		Settings loaded = Settings.Load();

		Assert.Equal(value, loaded.MspHints);
	}

	[Fact]
	public void Load_DefaultsMspHintsToAlternating_WhenFieldMissingFromJson() {
		EnsureAppDataDir();
		File.WriteAllText(Settings.SettingsPath, """
			{
				"Repetitions": 4
			}
			""");

		Settings result = Settings.Load();

		Assert.Equal(MspHints.ALTERNATING, result.MspHints);
	}

	[Fact]
	public void Save_SerializesMspHintsAsInteger_NotName() {
		Settings settings = NewSettings();
		settings.MspHints = MspHints.ALTERNATING_REVERSED;

		settings.Save();

		string json = File.ReadAllText(Settings.SettingsPath);
		Assert.Contains("\"MspHints\": 3", json);
		Assert.DoesNotContain("ALTERNATING_REVERSED", json);
	}

	#endregion

	#region PersistedSequence / PersistedSet

	[Fact]
	public void PersistedSequence_DefaultsToEmptyNameAndEmptySets() {
		HuntingSequence seq = new();

		Assert.Equal(0L, seq.Id);
		Assert.Equal("", seq.Name);
		Assert.NotNull(seq.Sets);
		Assert.Empty(seq.Sets);
	}

	[Fact]
	public void PersistedSet_DefaultsToZeroIds() {
		HuntingSet set = new();

		Assert.Equal(0, set.P1Id);
		Assert.Equal(0, set.P2Id);
		Assert.Equal(0, set.P3Id);
	}

	#endregion

	#region helpers

#pragma warning disable CS0618
	private static Settings NewSettings() {
		return new Settings();
	}
#pragma warning restore CS0618

	private static void AssertHasDefaults(Settings settings) {
		Assert.Equal(MspHints.ALTERNATING, settings.MspHints);
		Assert.False(settings.BackToMenu);
		Assert.True(settings.TimerReset);
		Assert.False(settings.RepetitionsInPlace);
		Assert.Equal(3, settings.Repetitions);
		Assert.NotNull(settings.CustomSequences);
		Assert.Empty(settings.CustomSequences);
		Assert.Equal(1L, settings.NextSequenceId);
	}

	private static void EnsureAppDataDir() {
		if (!Directory.Exists(Settings.AppDataPath)) {
			Directory.CreateDirectory(Settings.AppDataPath);
		}
	}

	#endregion
}
