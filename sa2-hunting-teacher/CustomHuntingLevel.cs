namespace sa2_hunting_teacher;

public class CustomHuntingLevel : HuntingLevel {
	private string Name;
	private Level Level;

	public override Dictionary<int, string> PieceToHintInstance { get; }


	public CustomHuntingLevel(SA2Manager manager, byte repetitions, HuntingSequence sequence, Dictionary<int, string> PieceToHint) : base(manager, repetitions) {
		this.Name = sequence.Name;
		this.Level = sequence.Level;
		this.PieceToHintInstance = PieceToHint;

		List<Set> sets = new();
		foreach (HuntingSet set in sequence.Sets) {
			sets.Add(Set.Create(set.P1Id, set.P2Id, set.P3Id, PieceToHint));
		}

		this.Sequence = sets.ToArray();
	}

	public override LevelId LevelId => SupportedLevels.LevelToLevelId[this.Level];

	protected override Set[] Sequence { get; }

	public override string ToString() => Name;
}
