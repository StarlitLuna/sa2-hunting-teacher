using System.Reflection;
using System.Text.Json;

namespace sa2_hunting_teacher {
	public partial class SetEditor : Form {
		private enum Slot {
			P1,
			P2,
			P3
		}

		private enum ComboOptionMode {
			Collapsed,
			Expanded
		}

		private sealed class CustomSequence {
			public long Id;
			public string Name = "";
			public Level? Level;
			public List<CustomSet> Sets = new();
		}

		private sealed class CustomSet {
			public int? P1Id;
			public int? P2Id;
			public int? P3Id;

			public bool IsEmpty {
				get {
					return this.P1Id is null && this.P2Id is null && this.P3Id is null;
				}
			}
		}

		private sealed class PieceOption {
			public int Id { get; }
			public string Hint { get; }

			public PieceOption(int id, string hint) {
				this.Id = id;
				this.Hint = hint;
			}

			public override string ToString() {
				return this.Hint;
			}
		}

		private sealed class NoneOption {
			public override string ToString() {
				return "(none)";
			}
		}

		private sealed class SlotDefinition {
			public required Slot Slot { get; init; }
			public required string Label { get; init; }
			public required Func<CustomSet, int?> GetValue { get; init; }
			public required Action<CustomSet, int?> SetValue { get; init; }
			public required Func<LevelCatalog, IReadOnlyList<int>> CatalogIds { get; init; }
			public required Func<DataRowControls, ComboBox> Combo { get; init; }
			public required bool AllowsEnemies { get; init; }
		}

		private static readonly NoneOption None = new();
		private static readonly SlotDefinition[] Slots = [
			new() {
				Slot = Slot.P1,
				Label = "Piece 1",
				GetValue = set => set.P1Id,
				SetValue = (set, value) => set.P1Id = value,
				CatalogIds = catalog => catalog.P1,
				Combo = row => row.P1,
				AllowsEnemies = true
			},
			new() {
				Slot = Slot.P2,
				Label = "Piece 2",
				GetValue = set => set.P2Id,
				SetValue = (set, value) => set.P2Id = value,
				CatalogIds = catalog => catalog.P2,
				Combo = row => row.P2,
				AllowsEnemies = true
			},
			new() {
				Slot = Slot.P3,
				Label = "Piece 3",
				GetValue = set => set.P3Id,
				SetValue = (set, value) => set.P3Id = value,
				CatalogIds = catalog => catalog.P3,
				Combo = row => row.P3,
				AllowsEnemies = false
			}
		];
		private static readonly Lazy<Dictionary<(LevelCatalog Catalog, Slot Slot), PieceOption[]>> SortedOptionCache =
			new(SetEditor.BuildSortedOptionCache);
		private static readonly object[] NoneOnlyOptions = [SetEditor.None];
		private const int AutoSaveDelayMs = 300;
		private readonly Dictionary<string, Dictionary<int, int[]>> Sets;

		private sealed class DataRowControls {
			public required CustomSet Model { get; init; }
			public required ComboBox P1 { get; init; }
			public required ComboBox P2 { get; init; }
			public required ComboBox P3 { get; init; }
			public required Button Delete { get; init; }
		}

		private readonly Settings settings;
		private readonly List<CustomSequence> sequences = new();
		private readonly List<DataRowControls> dataRows = new();
		private readonly System.Windows.Forms.Timer autoSaveTimer;
		private bool autoSavePending;
		private bool suspendEvents;
		private bool suspendSequenceSelection;

		public SetEditor(Settings settings, Dictionary<string, Dictionary<int, int[]>> sets) {
			InitializeComponent();
			this.settings = settings;
			this.Sets = sets;
			this.components ??= new System.ComponentModel.Container();
			this.autoSaveTimer = new System.Windows.Forms.Timer(this.components) {
				Interval = SetEditor.AutoSaveDelayMs
			};
			this.autoSaveTimer.Tick += this.autoSaveTimer_Tick;
			SupportedLevels.Configure(this.setEditorLevels);
			this.setEditorLevels.SelectedIndex = -1;
			this.LoadFromSettings();
			this.UpdateChromeForSelection();
		}

		private void LoadFromSettings() {
			this.customSequences.BeginUpdate();
			try {
				foreach (HuntingSequence sequence in this.settings.CustomSequences) {
					CustomSequence seq = SetEditor.FromPersisted(sequence);
					this.sequences.Add(seq);
					this.customSequences.Items.Add(new ListViewItem(seq.Name));
				}
			} finally {
				this.customSequences.EndUpdate();
			}
		}

		private static CustomSequence FromPersisted(HuntingSequence sequence) {
			CustomSequence seq = new() {
				Id = sequence.Id,
				Name = sequence.Name,
				Level = sequence.Level
			};

			foreach (HuntingSet s in sequence.Sets) {
				seq.Sets.Add(new CustomSet {
					P1Id = s.P1Id,
					P2Id = s.P2Id,
					P3Id = s.P3Id
				});
			}

			return seq;
		}

		private static HuntingSequence ToPersisted(CustomSequence seq) {
			HuntingSequence sequence = new() {
				Id = seq.Id,
				Name = seq.Name,
				Level = seq.Level!.Value
			};

			foreach (CustomSet set in seq.Sets) {
				if (set.IsEmpty) {
					continue;
				}

				sequence.Sets.Add(new HuntingSet {
					P1Id = set.P1Id!.Value,
					P2Id = set.P2Id!.Value,
					P3Id = set.P3Id!.Value
				});
			}

			return sequence;
		}

		private void ScheduleAutoSave() {
			this.autoSavePending = true;
			this.autoSaveTimer.Stop();
			this.autoSaveTimer.Start();
		}

		private void autoSaveTimer_Tick(object? sender, EventArgs e) {
			this.FlushAutoSave();
		}

		private void CancelPendingAutoSave() {
			this.autoSaveTimer.Stop();
			this.autoSavePending = false;
		}

		private void FlushAutoSave() {
			if (!this.autoSavePending) {
				return;
			}

			this.CancelPendingAutoSave();
			this.PersistIfValid(showErrors: false);
		}

		protected override void OnFormClosing(FormClosingEventArgs e) {
			this.FlushAutoSave();
			base.OnFormClosing(e);
		}

		protected override void OnFormClosed(FormClosedEventArgs e) {
			this.autoSaveTimer.Dispose();
			base.OnFormClosed(e);
		}

		private void PersistSequences() {
			List<HuntingSequence> persisted = new();
			foreach (CustomSequence seq in this.sequences) {
				persisted.Add(SetEditor.ToPersisted(seq));
			}

			this.settings.CustomSequences = persisted;
			this.settings.Save();
		}

		private bool PersistIfValid(bool showErrors) {
			IReadOnlyList<string> errors = this.ValidateSequences();
			if (errors.Count > 0) {
				if (showErrors) {
					MessageBox.Show(
						this,
						string.Join(Environment.NewLine, errors),
						"Cannot Save",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error
					);
				}

				return false;
			}

			this.PersistSequences();
			return true;
		}

		private void setEditorSave_Click(object sender, EventArgs e) {
			this.CancelPendingAutoSave();
			this.PersistIfValid(showErrors: true);
		}

		protected virtual DialogResult ConfirmDestructiveAction(string message, string caption) {
			return MessageBox.Show(
				this,
				message,
				caption,
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning
			);
		}

		private void importSetsBtn_Click(object sender, EventArgs e) {
			CustomSequence? current = this.Current();
			bool hasContent = current != null && current.Sets.Any(s => !s.IsEmpty);
			if (hasContent) {
				DialogResult choice = this.ConfirmDestructiveAction(
					"Importing sets will clear all rows in this sequence. Continue?",
					"Import Sets"
				);

				if (choice != DialogResult.Yes) {
					return;
				}
			}

			using SetImporter importer = new(this);
			importer.ShowDialog(this);
		}

		private CustomSequence? Current() {
			if (this.customSequences.SelectedIndices.Count != 1) {
				return null;
			}

			return this.sequences[this.customSequences.SelectedIndices[0]];
		}

		private void UpdateChromeForSelection() {
			CustomSequence? current = this.Current();
			this.setEditorLevels.Enabled = current != null;
			this.deleteSequence.Enabled = current != null;
			this.importSetsBtn.Enabled = current != null;
		}

		private void SetSelectedSequence(int? index) {
			this.suspendSequenceSelection = true;
			this.customSequences.BeginUpdate();
			try {
				this.customSequences.SelectedIndices.Clear();
				if (index.HasValue) {
					ListViewItem item = this.customSequences.Items[index.Value];
					item.Selected = true;
					item.Focused = true;
					item.EnsureVisible();
				}
			} finally {
				this.customSequences.EndUpdate();
				this.suspendSequenceSelection = false;
			}

			this.UpdateChromeForSelection();
			this.LoadSequenceIntoUi(this.Current());
		}

		private void addSequence_Click(object sender, EventArgs e) {
			CustomSequence seq = new() {
				Id = this.settings.NextSequenceId++,
				Name = $"Sequence {this.sequences.Count + 1}"
			};

			this.sequences.Add(seq);
			ListViewItem item = new(seq.Name);
			this.customSequences.BeginUpdate();
			try {
				this.customSequences.Items.Add(item);
			} finally {
				this.customSequences.EndUpdate();
			}
			this.SetSelectedSequence(this.sequences.Count - 1);
			this.customSequences.Focus();
			item.BeginEdit();
			this.ScheduleAutoSave();
		}

		private void deleteSequence_Click(object sender, EventArgs e) {
			this.DeleteSelectedSequence();
		}

		private void customSequences_KeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Delete) {
				this.DeleteSelectedSequence();
				e.Handled = true;
			}
		}

		private void DeleteSelectedSequence() {
			CustomSequence? current = this.Current();
			if (current == null) {
				return;
			}

			DialogResult choice = this.ConfirmDestructiveAction(
				$"Delete sequence \"{current.Name}\"?",
				"Delete Sequence"
			);
			if (choice != DialogResult.Yes) {
				return;
			}

			int index = this.customSequences.SelectedIndices[0];
			this.sequences.RemoveAt(index);
			this.suspendSequenceSelection = true;
			this.customSequences.BeginUpdate();
			try {
				this.customSequences.Items.RemoveAt(index);
			} finally {
				this.customSequences.EndUpdate();
				this.suspendSequenceSelection = false;
			}

			int? next = this.sequences.Count > 0
				? Math.Min(index, this.sequences.Count - 1)
				: null;
			this.SetSelectedSequence(next);
			this.CancelPendingAutoSave();
			this.PersistIfValid(showErrors: false);
		}

		private void customSequences_AfterLabelEdit(object sender, LabelEditEventArgs e) {
			if (string.IsNullOrWhiteSpace(e.Label)) {
				e.CancelEdit = true;
				return;
			}

			string trimmed = e.Label.Trim();
			this.sequences[e.Item].Name = trimmed;
			if (trimmed != e.Label) {
				e.CancelEdit = true;
				this.customSequences.Items[e.Item].Text = trimmed;
			}

			this.ScheduleAutoSave();
		}

		private void customSequences_SelectedIndexChanged(object sender, EventArgs e) {
			if (this.suspendSequenceSelection) {
				return;
			}

			this.UpdateChromeForSelection();
			this.LoadSequenceIntoUi(this.Current());
		}

		private void setEditorLevels_SelectedIndexChanged(object sender, EventArgs e) {
			if (this.suspendEvents) {
				return;
			}

			CustomSequence? current = this.Current();
			if (current == null) {
				return;
			}

			Level? newLevel = this.setEditorLevels.SelectedValue as Level?;
			if (newLevel == null || newLevel == current.Level) {
				return;
			}

			bool hasContent = current.Sets.Any(s => !s.IsEmpty);
			if (hasContent) {
				DialogResult choice = this.ConfirmDestructiveAction(
					"Changing the level will clear all rows in this sequence. Continue?",
					"Change Level"
				);

				if (choice != DialogResult.Yes) {
					this.suspendEvents = true;
					try {
						if (current.Level.HasValue) {
							this.setEditorLevels.SelectedValue = current.Level.Value;
						} else {
							this.setEditorLevels.SelectedIndex = -1;
						}
					} finally {
						this.suspendEvents = false;
					}
					return;
				}
			}

			current.Level = newLevel;
			current.Sets.Clear();
			this.LoadSequenceIntoUi(current);
			this.ScheduleAutoSave();
			if (hasContent) {
				this.CancelPendingAutoSave();
				this.PersistIfValid(showErrors: false);
			}
		}

		private void LoadSequenceIntoUi(CustomSequence? current) {
			this.suspendEvents = true;
			try {
				this.WithSuspendedRowLayout(() => {
					this.ClearDataRows();

					if (current == null) {
						this.setEditorLevels.SelectedIndex = -1;
						return;
					}

					if (current.Level.HasValue) {
						this.setEditorLevels.SelectedValue = current.Level.Value;
					} else {
						this.setEditorLevels.SelectedIndex = -1;
					}

					if (!current.Level.HasValue) {
						return;
					}

					LevelCatalog catalog = LevelCatalog.Get(current.Level.Value);
					foreach (CustomSet set in current.Sets) {
						this.AddDataRow(set, catalog);
					}

					this.EnsureTrailingEmptyRow(current, catalog);
				});
			} finally {
				this.suspendEvents = false;
			}
		}

		private void WithSuspendedRowLayout(Action action) {
			this.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			try {
				action();
			} finally {
				this.tableLayoutPanel1.ResumeLayout(true);
				this.splitContainer2.Panel2.ResumeLayout(false);
				this.splitContainer2.Panel2.PerformLayout();
				this.ResumeLayout(false);
			}
		}

		private void ClearDataRows() {
			foreach (DataRowControls row in this.dataRows) {
				row.P1.Dispose();
				row.P2.Dispose();
				row.P3.Dispose();
				row.Delete.Dispose();
			}

			this.dataRows.Clear();
			this.tableLayoutPanel1.RowStyles.Clear();
			this.tableLayoutPanel1.RowStyles.Add(new RowStyle());
			this.tableLayoutPanel1.RowCount = 1;
		}

		private void AddDataRow(CustomSet model, LevelCatalog catalog) {
			int rowIndex = this.tableLayoutPanel1.RowCount;
			this.tableLayoutPanel1.RowCount = rowIndex + 1;
			this.tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			ComboBox p1 = this.MakeSlotCombo();
			ComboBox p2 = this.MakeSlotCombo();
			ComboBox p3 = this.MakeSlotCombo();
			Button delete = new() {
				Text = "X",
				Anchor = AnchorStyles.None,
				AutoSize = false,
				Margin = new Padding(2),
				Size = new Size(24, 24),
				UseVisualStyleBackColor = true
			};

			DataRowControls row = new() {
				Model = model,
				P1 = p1,
				P2 = p2,
				P3 = p3,
				Delete = delete
			};
			this.dataRows.Add(row);

			foreach (SlotDefinition slot in SetEditor.Slots) {
				ComboBox combo = slot.Combo(row);
				this.PopulateRowCombo(row, slot, model, catalog, ComboOptionMode.Collapsed);
				combo.DropDown += (s, e) => this.OnRowComboDropDown(row, slot.Slot);
				combo.SelectedIndexChanged += (s, e) => this.OnRowComboChanged(row, slot.Slot);
			}
			delete.Click += (s, e) => this.OnRowDeleteClicked(row);

			this.tableLayoutPanel1.Controls.Add(p1, 0, rowIndex);
			this.tableLayoutPanel1.Controls.Add(p2, 1, rowIndex);
			this.tableLayoutPanel1.Controls.Add(p3, 2, rowIndex);
			this.tableLayoutPanel1.Controls.Add(delete, 3, rowIndex);
		}

		private void EnsureTrailingEmptyRow(CustomSequence current) {
			if (!current.Level.HasValue) {
				return;
			}

			this.EnsureTrailingEmptyRow(current, LevelCatalog.Get(current.Level.Value));
		}

		private void EnsureTrailingEmptyRow(CustomSequence current, LevelCatalog catalog) {
			if (current.Sets.Count > 0 && current.Sets[^1].IsEmpty) {
				return;
			}

			CustomSet trailing = new();
			current.Sets.Add(trailing);
			this.AddDataRow(trailing, catalog);
		}

		private void RemoveDataRowAt(int rowIndex) {
			DataRowControls row = this.dataRows[rowIndex];
			this.tableLayoutPanel1.Controls.Remove(row.P1);
			this.tableLayoutPanel1.Controls.Remove(row.P2);
			this.tableLayoutPanel1.Controls.Remove(row.P3);
			this.tableLayoutPanel1.Controls.Remove(row.Delete);
			row.P1.Dispose();
			row.P2.Dispose();
			row.P3.Dispose();
			row.Delete.Dispose();

			this.dataRows.RemoveAt(rowIndex);
			int tableRow = rowIndex + 1;
			if (this.tableLayoutPanel1.RowStyles.Count > tableRow) {
				this.tableLayoutPanel1.RowStyles.RemoveAt(tableRow);
			}

			for (int i = rowIndex; i < this.dataRows.Count; i++) {
				DataRowControls shifted = this.dataRows[i];
				int shiftedTableRow = i + 1;
				this.tableLayoutPanel1.SetRow(shifted.P1, shiftedTableRow);
				this.tableLayoutPanel1.SetRow(shifted.P2, shiftedTableRow);
				this.tableLayoutPanel1.SetRow(shifted.P3, shiftedTableRow);
				this.tableLayoutPanel1.SetRow(shifted.Delete, shiftedTableRow);
			}

			this.tableLayoutPanel1.RowCount = this.dataRows.Count + 1;
		}

		private ComboBox MakeSlotCombo() {
			return new ComboBox {
				DropDownStyle = ComboBoxStyle.DropDownList,
				Dock = DockStyle.Fill,
				Margin = new Padding(2),
				DropDownWidth = 400,
				Tag = ComboOptionMode.Collapsed
			};
		}

		private void OnRowComboDropDown(DataRowControls row, Slot slot) {
			CustomSequence? current = this.Current();
			if (current == null || !current.Level.HasValue) {
				return;
			}

			LevelCatalog catalog = LevelCatalog.Get(current.Level.Value);
			this.suspendEvents = true;
			try {
				this.PopulateRowCombo(row, SetEditor.DefinitionFor(slot), row.Model, catalog, ComboOptionMode.Expanded);
			} finally {
				this.suspendEvents = false;
			}
		}

		private void PopulateRowCombo(
			DataRowControls row,
			SlotDefinition slot,
			CustomSet model,
			LevelCatalog catalog,
			ComboOptionMode mode
		) {
			object[] items = mode == ComboOptionMode.Expanded
				? SetEditor.BuildExpandedOptions(catalog, slot, model)
				: SetEditor.BuildCollapsedOptionsForSlot(catalog, slot, model);

			ComboBox combo = slot.Combo(row);
			combo.BeginUpdate();
			try {
				combo.Tag = mode;
				combo.Items.Clear();
				combo.Items.AddRange(items);

				int? selected = slot.GetValue(model);
				combo.SelectedIndex = 0;
				if (selected.HasValue) {
					for (int i = 0; i < combo.Items.Count; i++) {
						if (combo.Items[i] is PieceOption opt && opt.Id == selected.Value) {
							combo.SelectedIndex = i;
							break;
						}
					}
				}
			} finally {
				combo.EndUpdate();
			}
		}

		private static object[] BuildCollapsedOptionsForSlot(LevelCatalog catalog, SlotDefinition slot, CustomSet model) {
			int? selected = slot.GetValue(model);
			if (!selected.HasValue || SetEditor.EnemyUsedInOtherSlot(catalog, slot, model, selected.Value)) {
				return SetEditor.NoneOnlyOptions;
			}

			PieceOption? option = SetEditor.OptionFor(catalog, slot, selected.Value);
			if (option == null) {
				return SetEditor.NoneOnlyOptions;
			}

			return [SetEditor.None, option];
		}

		private static object[] BuildOptions(LevelCatalog catalog, Slot slot, CustomSet model) {
			return SetEditor.BuildExpandedOptions(catalog, SetEditor.DefinitionFor(slot), model);
		}

		private static object[] BuildExpandedOptions(LevelCatalog catalog, SlotDefinition slot, CustomSet model) {
			HashSet<int> usedEnemiesElsewhere = new();
			foreach (SlotDefinition other in SetEditor.Slots) {
				if (other == slot) {
					continue;
				}

				int? id = other.GetValue(model);
				if (id.HasValue && SetEditor.IsEnemy(catalog, id.Value)) {
					usedEnemiesElsewhere.Add(id.Value);
				}
			}

			List<object> items = new();
			items.Add(SetEditor.None);
			foreach (PieceOption piece in SetEditor.SortedOptionsFor(catalog, slot)) {
				if (usedEnemiesElsewhere.Contains(piece.Id) && catalog.Enemies.Contains(piece.Id)) {
					continue;
				}

				items.Add(piece);
			}

			return items.ToArray();
		}

		private static bool EnemyUsedInOtherSlot(LevelCatalog catalog, Slot slot, CustomSet model, int id) {
			return SetEditor.EnemyUsedInOtherSlot(catalog, SetEditor.DefinitionFor(slot), model, id);
		}

		private static bool EnemyUsedInOtherSlot(LevelCatalog catalog, SlotDefinition slot, CustomSet model, int id) {
			if (!SetEditor.IsEnemy(catalog, id)) {
				return false;
			}

			foreach (SlotDefinition other in SetEditor.Slots) {
				if (other == slot) {
					continue;
				}

				if (other.GetValue(model) == id) {
					return true;
				}
			}

			return false;
		}

		private static PieceOption? OptionFor(LevelCatalog catalog, Slot slot, int id) {
			return SetEditor.OptionFor(catalog, SetEditor.DefinitionFor(slot), id);
		}

		private static PieceOption? OptionFor(LevelCatalog catalog, SlotDefinition slot, int id) {
			foreach (PieceOption option in SetEditor.SortedOptionsFor(catalog, slot)) {
				if (option.Id == id) {
					return option;
				}
			}

			return null;
		}

		private static PieceOption[] SortedOptionsFor(LevelCatalog catalog, SlotDefinition slot) {
			return SetEditor.SortedOptionCache.Value[(catalog, slot.Slot)];
		}

		private static Dictionary<(LevelCatalog Catalog, Slot Slot), PieceOption[]> BuildSortedOptionCache() {
			Dictionary<(LevelCatalog Catalog, Slot Slot), PieceOption[]> cache = new();
			foreach (Level level in Enum.GetValues<Level>()) {
				LevelCatalog catalog = LevelCatalog.Get(level);
				foreach (SlotDefinition slot in SetEditor.Slots) {
					List<PieceOption> pieces = new();
					foreach (int id in slot.CatalogIds(catalog)) {
						pieces.Add(new PieceOption(id, catalog.HintFor(id)));
					}

					if (slot.AllowsEnemies) {
						foreach (int id in catalog.Enemies) {
							pieces.Add(new PieceOption(id, catalog.HintFor(id)));
						}
					}

					pieces.Sort((a, b) => string.Compare(a.Hint, b.Hint, StringComparison.InvariantCultureIgnoreCase));
					cache[(catalog, slot.Slot)] = pieces.ToArray();
				}
			}

			return cache;
		}

		private static bool ShouldRefreshSiblingOptions(LevelCatalog catalog, int? oldId, int? newId) {
			if (oldId == newId) {
				return false;
			}

			bool oldWasEnemy = oldId.HasValue && SetEditor.IsEnemy(catalog, oldId.Value);
			bool newIsEnemy = newId.HasValue && SetEditor.IsEnemy(catalog, newId.Value);
			return oldWasEnemy || newIsEnemy;
		}

		private static bool IsEnemy(LevelCatalog catalog, int id) {
			return catalog.Enemies.Contains(id);
		}

		private static SlotDefinition DefinitionFor(Slot slot) {
			foreach (SlotDefinition definition in SetEditor.Slots) {
				if (definition.Slot == slot) {
					return definition;
				}
			}

			throw new ArgumentOutOfRangeException(nameof(slot));
		}

		private static ComboOptionMode ComboMode(ComboBox combo) {
			return combo.Tag is ComboOptionMode mode ? mode : ComboOptionMode.Collapsed;
		}

		private void OnRowComboChanged(DataRowControls row, Slot slot) {
			if (this.suspendEvents) {
				return;
			}

			CustomSequence? current = this.Current();
			if (current == null) {
				return;
			}

			int rowIndex = current.Sets.IndexOf(row.Model);
			if (rowIndex < 0 || rowIndex >= current.Sets.Count) {
				return;
			}

			CustomSet model = row.Model;
			SlotDefinition definition = SetEditor.DefinitionFor(slot);
			int? oldId = definition.GetValue(model);
			ComboBox combo = definition.Combo(row);
			int? newId = combo.SelectedItem is PieceOption opt ? opt.Id : (int?)null;
			definition.SetValue(model, newId);

			if (!current.Level.HasValue) {
				return;
			}

			LevelCatalog catalog = LevelCatalog.Get(current.Level.Value);
			if (SetEditor.ShouldRefreshSiblingOptions(catalog, oldId, newId)) {
				this.suspendEvents = true;
				try {
					foreach (SlotDefinition other in SetEditor.Slots) {
						if (other == definition) {
							continue;
						}

						ComboBox otherCombo = other.Combo(row);
						this.PopulateRowCombo(row, other, model, catalog, SetEditor.ComboMode(otherCombo));
					}
				} finally {
					this.suspendEvents = false;
				}
			}

			if (rowIndex == current.Sets.Count - 1 && !model.IsEmpty) {
				this.WithSuspendedRowLayout(() => this.EnsureTrailingEmptyRow(current, catalog));
			}

			this.ScheduleAutoSave();
		}

		public IReadOnlyList<string> ValidateSequences() {
			List<string> errors = new();
			foreach (CustomSequence seq in this.sequences) {
				string seqLabel = $"\"{seq.Name}\"";
				if (!seq.Level.HasValue) {
					errors.Add($"{seqLabel}: no level selected.");
					continue;
				}

				LevelCatalog catalog = LevelCatalog.Get(seq.Level.Value);
				for (int rowIdx = 0; rowIdx < seq.Sets.Count; rowIdx++) {
					CustomSet set = seq.Sets[rowIdx];
					if (set.IsEmpty) {
						continue;
					}

					this.ValidateRow(catalog, set, seqLabel, rowIdx + 1, errors);
				}
			}

			return errors;
		}

		private void ValidateRow(LevelCatalog catalog, CustomSet set, string seqLabel, int rowNum, List<string> errors) {
			foreach (SlotDefinition slot in SetEditor.Slots) {
				int? id = slot.GetValue(set);
				if (!id.HasValue) {
					errors.Add($"{seqLabel} row {rowNum}: {slot.Label} is not set.");
					continue;
				}

				if (!SetEditor.IsValidForSlot(catalog, slot, id.Value)) {
					errors.Add($"{seqLabel} row {rowNum}: {slot.Label} has id 0x{id.Value:X4} which is not a {SetEditor.ValidSlotDescription(slot)}.");
				}
			}

			HashSet<int> seen = new();
			HashSet<int> reported = new();
			foreach (SlotDefinition slot in SetEditor.Slots) {
				int? id = slot.GetValue(set);
				if (!id.HasValue) {
					continue;
				}

				if (!seen.Add(id.Value) && reported.Add(id.Value)) {
					errors.Add($"{seqLabel} row {rowNum}: piece id 0x{id.Value:X4} appears more than once.");
				}
			}
		}

		private static bool IsValidForSlot(LevelCatalog catalog, SlotDefinition slot, int id) {
			return slot.CatalogIds(catalog).Contains(id) || (slot.AllowsEnemies && SetEditor.IsEnemy(catalog, id));
		}

		private static string ValidSlotDescription(SlotDefinition slot) {
			return slot.AllowsEnemies
				? $"valid {slot.Label} or Enemy id"
				: $"valid {slot.Label} id";
		}

		private void OnRowDeleteClicked(DataRowControls row) {
			CustomSequence? current = this.Current();
			if (current == null) {
				return;
			}

			int rowIndex = current.Sets.IndexOf(row.Model);
			if (rowIndex < 0 || rowIndex >= current.Sets.Count) {
				return;
			}

			CustomSet model = row.Model;
			if (!model.IsEmpty) {
				DialogResult choice = this.ConfirmDestructiveAction(
					"Delete this row?",
					"Delete Row"
				);

				if (choice != DialogResult.Yes) {
					return;
				}
			}

			this.WithSuspendedRowLayout(() => {
				current.Sets.RemoveAt(rowIndex);
				this.RemoveDataRowAt(rowIndex);
				this.EnsureTrailingEmptyRow(current);
			});
			this.CancelPendingAutoSave();
			this.PersistIfValid(showErrors: false);
		}

		public void importSets(int[] sets, bool storyStyle) {
			CustomSequence? current = this.Current();
			if (current == null || !current.Level.HasValue) {
				return;
			}

			LevelCatalog catalog = LevelCatalog.Get(current.Level.Value);
			string levelId = "" + ((int)SupportedLevels.LevelToLevelId[current.Level.Value]);
			if (storyStyle && this.Sets.ContainsKey(levelId + "-story")) {
				levelId += "-story";
			}

			this.WithSuspendedRowLayout(() => {
				current.Sets.Clear();
				this.ClearDataRows();

				foreach (int setId in sets) {
					int[] pieces = this.Sets[levelId][setId];
					CustomSet set = new() {
						P1Id = pieces[0],
						P2Id = pieces[1],
						P3Id = pieces[2]
					};

					current.Sets.Add(set);
					this.AddDataRow(set, catalog);
				}

				this.EnsureTrailingEmptyRow(current, catalog);
			});

			this.ScheduleAutoSave();
		}
	}
}
