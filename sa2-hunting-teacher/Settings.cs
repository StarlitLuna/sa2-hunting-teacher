using System.Text.Json;

namespace sa2_hunting_teacher;

public class Settings {
	/// <summary>
	/// The default value of the Reversed Hints checkbox that appears for Mad Space
	/// When true (i.e. checked), the hints in Mad Space will be reversed (i.e. vanilla behavior)
	/// When false (i.e. not checked), the hints in Mad Space will not be reversed (i.e. the hints will appear left to right readable)
	/// Defaults to <c>false</c>.
	/// </summary>
	public bool MspReversedHints { get; set; } = false;

	/// <summary>
	/// Whether or not you will go back to the stage select menu after completing a set.
	/// When true (i.e. checked), you will return to stage select after collecting the third piece.
	/// When false (i.e. not checked), you will respawn and get your next set without exiting the level.
	/// Defaults to <c>false</c>.
	/// </summary>
	public bool BackToMenu { get; set; } = false;

	/// <summary>
	/// Whether or not the in-game timer will reset to 0 after you collect your third piece. Does nothing if Back To Menu is on.
	/// When true (i.e. checked), the in game timer will reset after you collect your third piece in a set.
	/// When false (i.e. not checked), the in game timer will keep going through set changes.
	/// Defaults to <c>true</c>.
	/// </summary>
	public bool TimerReset { get; set; } = true;

	/// <summary>
	/// Whether or not repetitions happen at the end of a sequence or in place per set.
	/// When true (i.e. checked), you will play the same set x times before proceeding to the next set.
	/// When false (i.e. not checked), you will play one set at a time and the sequence itself will repeat x times.
	/// Defaults to <c>false</c>.
	/// </summary>
	public bool RepetitionsInPlace { get; set; } = false;

	/// <summary>
	/// The number of repetitions to play in a sequence.
	/// Defaults to <c>3</c>.
	/// </summary>
	public int Repetitions { get; set; } = 3;

	/// <summary>
	/// User-authored custom sequences from the Set Editor.
	/// </summary>
	public List<HuntingSequence> CustomSequences { get; set; } = new();

	/// <summary>
	/// Monotonically increasing identifier handed out to each newly-added custom sequence.
	/// Starts at <c>1</c> and is never decremented even when sequences are deleted.
	/// </summary>
	public long NextSequenceId { get; set; } = 1;

	public static readonly string AppDataPath = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
		"SA2 Hunting Teacher"
	);

	public static readonly string SettingsPath = Path.Combine(
		Settings.AppDataPath,
		"settings.json"
	);

	private static readonly JsonSerializerOptions JSONOptions = new() {
		PropertyNameCaseInsensitive = false,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true,
		WriteIndented = true
	};

	[System.Obsolete("Do not use this constructor directly. Use Settings.Load() instead.", false)]
	public Settings() { }

	public static Settings Load() {
#pragma warning disable CS0618
		if (!Path.Exists(Settings.AppDataPath)) {
			Directory.CreateDirectory(Settings.AppDataPath);
			return new Settings();
		}

		if (!Path.Exists(Settings.SettingsPath)) {
			return new Settings();
		}

		return JsonSerializer.Deserialize<Settings>(
			File.ReadAllText(Settings.SettingsPath),
			Settings.JSONOptions
		) ?? new Settings();
#pragma warning restore CS0618
	}

	public void Save() {
		Directory.CreateDirectory(Settings.AppDataPath);
		File.WriteAllText(
			Settings.SettingsPath,
			JsonSerializer.Serialize<Settings>(this, Settings.JSONOptions)
		);
	}
}

public class HuntingSequence {
	public long Id { get; set; }
	public string Name { get; set; } = "";
	public Level Level { get; set; }
	public List<HuntingSet> Sets { get; set; } = new();
}

public class HuntingSet {
	public int P1Id { get; set; }
	public int P2Id { get; set; }
	public int P3Id { get; set; }
}
