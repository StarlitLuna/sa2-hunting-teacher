using sa2_hunting_teacher;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace unit_tests;

public class SetEditorValidationTests {
	private static readonly Type SetEditorType = typeof(SetEditor);
	private static readonly Type CustomSequenceType =
		SetEditorType.GetNestedType("CustomSequence", BindingFlags.NonPublic)!;
	private static readonly Type CustomSetType =
		SetEditorType.GetNestedType("CustomSet", BindingFlags.NonPublic)!;
	private static readonly Type SequenceListType =
		typeof(List<>).MakeGenericType(CustomSequenceType);
	private static readonly Type SetListType =
		typeof(List<>).MakeGenericType(CustomSetType);

	#region Empty / sequence-level errors

	[Fact]
	public void Validate_NoSequences_ReturnsEmpty() {
		SetEditor editor = NewEditor();
		SetSequences(editor, Array.Empty<object>());

		Assert.Empty(editor.ValidateSequences());
	}

	[Fact]
	public void Validate_SequenceWithoutLevel_ReportsNoLevelSelected() {
		SetEditor editor = NewEditor();
		object seq = NewSequence("Untitled", level: null);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Single(errors);
		Assert.Contains("\"Untitled\"", errors[0]);
		Assert.Contains("no level selected", errors[0]);
	}

	[Fact]
	public void Validate_SequenceWithLevelAndOnlyEmptyRows_ReturnsEmpty() {
		SetEditor editor = NewEditor();
		object seq = NewSequence("S", Level.AquaticMine, sets: [(null, null, null), (null, null, null)]);
		SetSequences(editor, [seq]);

		Assert.Empty(editor.ValidateSequences());
	}

	[Fact]
	public void Validate_SequenceWithLevelAndNoRows_ReturnsEmpty() {
		SetEditor editor = NewEditor();
		object seq = NewSequence("S", Level.AquaticMine);
		SetSequences(editor, [seq]);

		Assert.Empty(editor.ValidateSequences());
	}

	#endregion

	#region Row-level — missing slots

	[Fact]
	public void Validate_FullyValidRow_ReturnsEmpty() {
		SetEditor editor = NewEditor();
		(int p1, int p2, int p3) ids = ValidIds(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(ids.p1, ids.p2, ids.p3)]);
		SetSequences(editor, [seq]);

		Assert.Empty(editor.ValidateSequences());
	}

	[Fact]
	public void Validate_RowWithMissingP1_ReportsP1NotSet() {
		SetEditor editor = NewEditor();
		(int p1, int p2, int p3) ids = ValidIds(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(null, ids.p2, ids.p3)]);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Contains(errors, e => e.Contains("Piece 1 is not set"));
		Assert.DoesNotContain(errors, e => e.Contains("Piece 2 is not set"));
		Assert.DoesNotContain(errors, e => e.Contains("Piece 3 is not set"));
	}

	[Fact]
	public void Validate_RowWithMissingP2_ReportsP2NotSet() {
		SetEditor editor = NewEditor();
		(int p1, int p2, int p3) ids = ValidIds(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(ids.p1, null, ids.p3)]);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Contains(errors, e => e.Contains("Piece 2 is not set"));
	}

	[Fact]
	public void Validate_RowWithMissingP3_ReportsP3NotSet() {
		SetEditor editor = NewEditor();
		(int p1, int p2, int p3) ids = ValidIds(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(ids.p1, ids.p2, null)]);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Contains(errors, e => e.Contains("Piece 3 is not set"));
	}

	[Fact]
	public void Validate_RowWithTwoMissingSlots_ReportsBothSeparately() {
		SetEditor editor = NewEditor();
		(int p1, int p2, int p3) ids = ValidIds(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(ids.p1, null, null)]);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Contains(errors, e => e.Contains("Piece 2 is not set"));
		Assert.Contains(errors, e => e.Contains("Piece 3 is not set"));
	}

	#endregion

	#region Row-level — slot type

	[Fact]
	public void Validate_P2IdInP1Slot_ReportsInvalidForSlot() {
		SetEditor editor = NewEditor();
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);
		int p2Id = catalog.P2.First();
		object seq = NewSequence("S", Level.AquaticMine, sets: [(p2Id, catalog.P2.First(), catalog.P3.First())]);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Contains(errors, e => e.Contains("Piece 1 has id"));
	}

	[Fact]
	public void Validate_EnemyInP1Slot_IsValid() {
		SetEditor editor = NewEditor();
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(catalog.Enemies.First(), catalog.P2.First(), catalog.P3.First())]);
		SetSequences(editor, [seq]);

		Assert.Empty(editor.ValidateSequences());
	}

	[Fact]
	public void Validate_EnemyInP2Slot_IsValid() {
		SetEditor editor = NewEditor();
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(catalog.P1.First(), catalog.Enemies.First(), catalog.P3.First())]);
		SetSequences(editor, [seq]);

		Assert.Empty(editor.ValidateSequences());
	}

	[Fact]
	public void Validate_EnemyInP3Slot_ReportsInvalidForSlot() {
		SetEditor editor = NewEditor();
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(catalog.P1.First(), catalog.P2.First(), catalog.Enemies.First())]);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Contains(errors, e => e.Contains("Piece 3 has id"));
	}

	[Fact]
	public void Validate_UnknownIdInSlot_ReportsInvalid() {
		SetEditor editor = NewEditor();
		(int p1, int p2, int p3) ids = ValidIds(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(ids.p1, ids.p2, 0xDEAD)]);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Contains(errors, e => e.Contains("Piece 3 has id 0xDEAD"));
	}

	#endregion

	#region Row-level — duplicates

	[Fact]
	public void Validate_DuplicateEnemyAcrossTwoSlots_ReportsOnce() {
		SetEditor editor = NewEditor();
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);
		int enemy = catalog.Enemies.First();
		object seq = NewSequence("S", Level.AquaticMine, sets: [(enemy, enemy, catalog.P3.First())]);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		int matches = errors.Count(e => e.Contains("appears more than once"));
		Assert.Equal(1, matches);
	}

	[Fact]
	public void Validate_DuplicateEnemyAcrossThreeSlots_ReportsOnce() {
		SetEditor editor = NewEditor();
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);
		int enemy = catalog.Enemies.First();
		object seq = NewSequence("S", Level.AquaticMine, sets: [(enemy, enemy, enemy)]);
		SetSequences(editor, [seq]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		int matches = errors.Count(e => e.Contains("appears more than once"));
		Assert.Equal(1, matches);
	}

	[Fact]
	public void Validate_DuplicateAcrossRows_IsAllowed() {
		SetEditor editor = NewEditor();
		(int p1, int p2, int p3) ids = ValidIds(Level.AquaticMine);
		object seq = NewSequence("S", Level.AquaticMine, sets: [(ids.p1, ids.p2, ids.p3), (ids.p1, ids.p2, ids.p3)]);
		SetSequences(editor, [seq]);

		Assert.Empty(editor.ValidateSequences());
	}

	#endregion

	#region Multi-sequence

	[Fact]
	public void Validate_MultipleSequences_EachReportsErrorsIndependently() {
		SetEditor editor = NewEditor();
		object seqA = NewSequence("Bad1", level: null);
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);
		int enemy = catalog.Enemies.First();
		object seqB = NewSequence("Bad2", Level.AquaticMine, sets: [(enemy, enemy, catalog.P3.First())]);
		SetSequences(editor, [seqA, seqB]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Contains(errors, e => e.Contains("\"Bad1\"") && e.Contains("no level selected"));
		Assert.Contains(errors, e => e.Contains("\"Bad2\"") && e.Contains("appears more than once"));
	}

	[Fact]
	public void Validate_OneInvalidSequence_DoesNotMaskValidOnes() {
		SetEditor editor = NewEditor();
		(int p1, int p2, int p3) ids = ValidIds(Level.AquaticMine);
		object good = NewSequence("Good", Level.AquaticMine, sets: [(ids.p1, ids.p2, ids.p3)]);
		object bad = NewSequence("Bad", level: null);
		SetSequences(editor, [good, bad]);

		IReadOnlyList<string> errors = editor.ValidateSequences();

		Assert.Single(errors);
		Assert.Contains("\"Bad\"", errors[0]);
	}

	#endregion

	#region helpers

	private static SetEditor NewEditor() {
		return (SetEditor)RuntimeHelpers.GetUninitializedObject(typeof(SetEditor));
	}

	private static (int p1, int p2, int p3) ValidIds(Level level) {
		LevelCatalog catalog = LevelCatalog.Get(level);
		return (catalog.P1.First(), catalog.P2.First(), catalog.P3.First());
	}

	private static void SetSequences(SetEditor editor, IEnumerable<object> sequences) {
		FieldInfo field = SetEditorType.GetField("sequences", BindingFlags.Instance | BindingFlags.NonPublic)!;
		IList list = (IList)Activator.CreateInstance(SequenceListType)!;
		foreach (object seq in sequences) {
			list.Add(seq);
		}
		field.SetValue(editor, list);
	}

	private static object NewSequence(string name, Level? level = null, params (int? p1, int? p2, int? p3)[] sets) {
		object seq = Activator.CreateInstance(CustomSequenceType)!;
		CustomSequenceType.GetField("Name")!.SetValue(seq, name);
		CustomSequenceType.GetField("Level")!.SetValue(seq, level);

		IList setsList = (IList)Activator.CreateInstance(SetListType)!;
		foreach ((int? p1, int? p2, int? p3) in sets) {
			object s = Activator.CreateInstance(CustomSetType)!;
			CustomSetType.GetField("P1Id")!.SetValue(s, p1);
			CustomSetType.GetField("P2Id")!.SetValue(s, p2);
			CustomSetType.GetField("P3Id")!.SetValue(s, p3);
			setsList.Add(s);
		}
		CustomSequenceType.GetField("Sets")!.SetValue(seq, setsList);

		return seq;
	}

	#endregion
}
