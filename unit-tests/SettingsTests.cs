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

		Assert.False(settings.MspReversedHints);
		Assert.False(settings.BackToMenu);
		Assert.True(settings.TimerReset);
		Assert.False(settings.RepititionsInPlace);
		Assert.Equal(3, settings.Repititions);
		Assert.NotNull(settings.CustomSequences);
		Assert.Empty(settings.CustomSequences);
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
		settings.MspReversedHints = true;
		settings.Repititions = 7;

		settings.Save();

		Assert.True(File.Exists(Settings.SettingsPath));
		string json = File.ReadAllText(Settings.SettingsPath);
		Assert.Contains("\"MspReversedHints\": true", json);
		Assert.Contains("\"Repititions\": 7", json);
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
		first.Repititions = 5;
		first.Save();

		Settings second = NewSettings();
		second.Repititions = 9;
		second.Save();

		string json = File.ReadAllText(Settings.SettingsPath);
		Assert.Contains("\"Repititions\": 9", json);
		Assert.DoesNotContain("\"Repititions\": 5", json);
	}

	[Fact]
	public void Save_SerializesAllProperties() {
		Settings settings = NewSettings();
		settings.MspReversedHints = true;
		settings.BackToMenu = true;
		settings.TimerReset = false;
		settings.RepititionsInPlace = true;
		settings.Repititions = 11;
		settings.CustomSequences.Add(new PersistedSequence {
			Name = "Test",
			Level = Level.WildCanyon,
			Sets = [new PersistedSet { P1Id = 1, P2Id = 2, P3Id = 3 }],
		});

		settings.Save();

		string json = File.ReadAllText(Settings.SettingsPath);
		Assert.Contains("\"MspReversedHints\": true", json);
		Assert.Contains("\"BackToMenu\": true", json);
		Assert.Contains("\"TimerReset\": false", json);
		Assert.Contains("\"RepititionsInPlace\": true", json);
		Assert.Contains("\"Repititions\": 11", json);
		Assert.Contains("\"CustomSequences\":", json);
		Assert.Contains("\"Name\": \"Test\"", json);
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
				"MspReversedHints": true,
				"BackToMenu": true,
				"TimerReset": false,
				"RepititionsInPlace": true,
				"Repititions": 13,
				"CustomSequences": [
					{
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

		Assert.True(result.MspReversedHints);
		Assert.True(result.BackToMenu);
		Assert.False(result.TimerReset);
		Assert.True(result.RepititionsInPlace);
		Assert.Equal(13, result.Repititions);
		Assert.Single(result.CustomSequences);
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
				"Repititions": 4,
				"BackToMenu": true,
			}
			""");

		Settings result = Settings.Load();

		Assert.Equal(4, result.Repititions);
		Assert.True(result.BackToMenu);
	}

	[Fact]
	public void Load_SkipsComments() {
		EnsureAppDataDir();
		File.WriteAllText(Settings.SettingsPath, """
			{
				// line comment
				"Repititions": 6,
				/* block comment */
				"TimerReset": false
			}
			""");

		Settings result = Settings.Load();

		Assert.Equal(6, result.Repititions);
		Assert.False(result.TimerReset);
	}

	[Fact]
	public void Load_IsCaseSensitive_ForPropertyNames() {
		EnsureAppDataDir();
		File.WriteAllText(Settings.SettingsPath, """
			{
				"mspreversedhints": true,
				"timerreset": false,
				"repititions": 99
			}
			""");

		Settings result = Settings.Load();

		Assert.False(result.MspReversedHints);
		Assert.True(result.TimerReset);
		Assert.Equal(3, result.Repititions);
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
		original.MspReversedHints = true;
		original.BackToMenu = true;
		original.TimerReset = false;
		original.RepititionsInPlace = true;
		original.Repititions = 17;
		original.CustomSequences.Add(new PersistedSequence {
			Name = "Round Trip",
			Level = Level.MadSpace,
			Sets = [
				new PersistedSet { P1Id = 0x1234, P2Id = 0x5678, P3Id = 0x9ABC },
				new PersistedSet { P1Id = 1, P2Id = 2, P3Id = 3 },
			],
		});

		original.Save();
		Settings loaded = Settings.Load();

		Assert.Equal(original.MspReversedHints, loaded.MspReversedHints);
		Assert.Equal(original.BackToMenu, loaded.BackToMenu);
		Assert.Equal(original.TimerReset, loaded.TimerReset);
		Assert.Equal(original.RepititionsInPlace, loaded.RepititionsInPlace);
		Assert.Equal(original.Repititions, loaded.Repititions);
		Assert.Equal(original.CustomSequences.Count, loaded.CustomSequences.Count);
		Assert.Equal(original.CustomSequences[0].Name, loaded.CustomSequences[0].Name);
		Assert.Equal(original.CustomSequences[0].Level, loaded.CustomSequences[0].Level);
		Assert.Equal(original.CustomSequences[0].Sets.Count, loaded.CustomSequences[0].Sets.Count);
		Assert.Equal(0x1234, loaded.CustomSequences[0].Sets[0].P1Id);
		Assert.Equal(0x5678, loaded.CustomSequences[0].Sets[0].P2Id);
		Assert.Equal(0x9ABC, loaded.CustomSequences[0].Sets[0].P3Id);
	}

	#endregion

	#region PersistedSequence / PersistedSet

	[Fact]
	public void PersistedSequence_DefaultsToEmptyNameAndEmptySets() {
		PersistedSequence seq = new();

		Assert.Equal("", seq.Name);
		Assert.NotNull(seq.Sets);
		Assert.Empty(seq.Sets);
	}

	[Fact]
	public void PersistedSet_DefaultsToZeroIds() {
		PersistedSet set = new();

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
		Assert.False(settings.MspReversedHints);
		Assert.False(settings.BackToMenu);
		Assert.True(settings.TimerReset);
		Assert.False(settings.RepititionsInPlace);
		Assert.Equal(3, settings.Repititions);
		Assert.NotNull(settings.CustomSequences);
		Assert.Empty(settings.CustomSequences);
	}

	private static void EnsureAppDataDir() {
		if (!Directory.Exists(Settings.AppDataPath)) {
			Directory.CreateDirectory(Settings.AppDataPath);
		}
	}

	#endregion
}
