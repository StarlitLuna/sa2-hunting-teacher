using sa2_hunting_teacher.Knuckles;

namespace sa2_hunting_teacher;

public enum Level {
	WildCanyon,
	PumpkinHill,
	AquaticMine,
	DeathChamber,
	MeteorHerd,
	DryLagoon,
	EggQuarters,
	SecurityHall,
	MadSpace
};

public abstract class HuntingLevel(SA2Manager manager, byte repetitions) {
	protected abstract Set[] Sequence { get; }
	public abstract LevelId LevelId { get; }

	private int Next = 0;
	private int SequenceCount = 0;

	protected SA2Manager Manager { get; } = manager;

	protected byte Repetitions { get; } = repetitions;

	protected byte Repetition { get; set; } = 0;

	public abstract Dictionary<int, string> PieceToHintInstance { get; }

	public bool SequenceComplete() {
		return SequenceCount >= this.Sequence.Length * this.Repetitions;
	}

	public bool SequenceWillBeComplete() {
		return SequenceCount >= (this.Sequence.Length * this.Repetitions) - 1;
	}

	public bool RunSequence() {
		if (!this.Manager.IsInWinScreen() && !this.Manager.IsLevelLoading()) {
			return false;
		}

		if (this.Manager.IsInWinScreen()) {
			this.Next = this.NextSequence();
			this.SequenceCount++;
		}

		this.Manager.ApplySet(
			this.Sequence[Next],
			this.Next + 1,
			this.Sequence.Length,
			!this.Manager.RepetitionsInPlace()
				? (int)Math.Ceiling((double)(this.SequenceCount + 1) / (double)this.Sequence.Length)
				: (this.Repetition + 1)
		);

		return true;
	}

	private int NextSequence() {
        if (this.Manager.RepetitionsInPlace()) {
            if (++this.Repetition < this.Repetitions) {
				return this.Next;
			}

			this.Repetition = 0;
        }

        return this.Next + 1 >= this.Sequence.Length ? 0 : this.Next + 1;
	}
}
