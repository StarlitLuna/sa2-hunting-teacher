namespace sa2_hunting_teacher {
	public partial class SetEditor : Form {
		private enum Slot {
			P1,
			P2,
			P3
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

			public int? GetSlot(Slot slot) {
				return slot switch {
					Slot.P1 => this.P1Id,
					Slot.P2 => this.P2Id,
					Slot.P3 => this.P3Id,
					_ => throw new ArgumentOutOfRangeException(nameof(slot))
				};
			}

			public void SetSlot(Slot slot, int? value) {
				switch (slot) {
					case Slot.P1:
						this.P1Id = value;
						break;
					case Slot.P2:
						this.P2Id = value;
						break;
					case Slot.P3:
						this.P3Id = value;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(slot));
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

		private static readonly NoneOption None = new();

		private sealed class DataRowControls {
			public required ComboBox P1 { get; init; }
			public required ComboBox P2 { get; init; }
			public required ComboBox P3 { get; init; }
			public required Button Delete { get; init; }

			public ComboBox GetCombo(Slot slot) {
				return slot switch {
					Slot.P1 => this.P1,
					Slot.P2 => this.P2,
					Slot.P3 => this.P3,
					_ => throw new ArgumentOutOfRangeException(nameof(slot))
				};
			}
		}

		private readonly Settings settings;
		private readonly List<CustomSequence> sequences = new();
		private readonly List<DataRowControls> dataRows = new();
		private bool suspendEvents;

		public SetEditor(Settings settings) {
			InitializeComponent();
			this.settings = settings;
			SupportedLevels.Configure(this.setEditorLevels);
			this.setEditorLevels.SelectedIndex = -1;
			this.LoadFromSettings();
			this.UpdateChromeForSelection();
		}

		private void LoadFromSettings() {
			foreach (HuntingSequence sequence in this.settings.CustomSequences) {
				CustomSequence seq = SetEditor.FromPersisted(sequence);
				this.sequences.Add(seq);
				this.customSequences.Items.Add(new ListViewItem(seq.Name));
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

		private void TryAutoSave() {
			if (this.ValidateSequences().Count > 0) {
				return;
			}

			this.PersistSequences();
		}

		private void PersistSequences() {
			List<HuntingSequence> persisted = new();
			foreach (CustomSequence seq in this.sequences) {
				persisted.Add(SetEditor.ToPersisted(seq));
			}

			this.settings.CustomSequences = persisted;
			this.settings.Save();
		}

		private void setEditorSave_Click(object sender, EventArgs e) {
			IReadOnlyList<string> errors = this.ValidateSequences();
			if (errors.Count > 0) {
				MessageBox.Show(
					this,
					string.Join(Environment.NewLine, errors),
					"Cannot Save",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error
				);

				return;
			}

			this.PersistSequences();
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
		}

		private void addSequence_Click(object sender, EventArgs e) {
			CustomSequence seq = new() {
				Id = this.settings.NextSequenceId++,
				Name = $"Sequence {this.sequences.Count + 1}"
			};

			this.sequences.Add(seq);
			ListViewItem item = new(seq.Name);
			this.customSequences.Items.Add(item);
			item.Selected = true;
			this.customSequences.Focus();
			item.BeginEdit();
			this.TryAutoSave();
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

			DialogResult choice = MessageBox.Show(
				this,
				$"Delete sequence \"{current.Name}\"?",
				"Delete Sequence",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning
			);
			if (choice != DialogResult.Yes) {
				return;
			}

			int index = this.customSequences.SelectedIndices[0];
			this.sequences.RemoveAt(index);
			this.customSequences.Items.RemoveAt(index);

			if (this.sequences.Count > 0) {
				int next = Math.Min(index, this.sequences.Count - 1);
				this.customSequences.Items[next].Selected = true;
			} else {
				this.LoadSequenceIntoUi(null);
			}

			this.TryAutoSave();
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

			this.TryAutoSave();
		}

		private void customSequences_SelectedIndexChanged(object sender, EventArgs e) {
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
				DialogResult choice = MessageBox.Show(
					this,
					"Changing the level will clear all rows in this sequence. Continue?",
					"Change Level",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning
				);

				if (choice != DialogResult.Yes) {
					this.suspendEvents = true;
					if (current.Level.HasValue) {
						this.setEditorLevels.SelectedValue = current.Level.Value;
					} else {
						this.setEditorLevels.SelectedIndex = -1;
					}

					this.suspendEvents = false;
					return;
				}
			}

			current.Level = newLevel;
			current.Sets.Clear();
			this.LoadSequenceIntoUi(current);
			this.TryAutoSave();
		}

		private void LoadSequenceIntoUi(CustomSequence? current) {
			this.suspendEvents = true;
			try {
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

				foreach (CustomSet set in current.Sets) {
					this.AddDataRow(set);
				}

				if (current.Sets.Count == 0 || !current.Sets[^1].IsEmpty) {
					CustomSet trailing = new();
					current.Sets.Add(trailing);
					this.AddDataRow(trailing);
				}
			} finally {
				this.suspendEvents = false;
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
			while (this.tableLayoutPanel1.RowStyles.Count > 1) {
				this.tableLayoutPanel1.RowStyles.RemoveAt(this.tableLayoutPanel1.RowStyles.Count - 1);
			}

			this.tableLayoutPanel1.RowCount = 1;
		}

		private void AddDataRow(CustomSet model) {
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
				P1 = p1,
				P2 = p2,
				P3 = p3,
				Delete = delete
			};
			this.dataRows.Add(row);

			this.PopulateRowCombo(row, Slot.P1, model);
			this.PopulateRowCombo(row, Slot.P2, model);
			this.PopulateRowCombo(row, Slot.P3, model);

			p1.SelectedIndexChanged += (s, e) => this.OnRowComboChanged(row, Slot.P1);
			p2.SelectedIndexChanged += (s, e) => this.OnRowComboChanged(row, Slot.P2);
			p3.SelectedIndexChanged += (s, e) => this.OnRowComboChanged(row, Slot.P3);
			delete.Click += (s, e) => this.OnRowDeleteClicked(row);

			this.tableLayoutPanel1.Controls.Add(p1, 0, rowIndex);
			this.tableLayoutPanel1.Controls.Add(p2, 1, rowIndex);
			this.tableLayoutPanel1.Controls.Add(p3, 2, rowIndex);
			this.tableLayoutPanel1.Controls.Add(delete, 3, rowIndex);
		}

		private ComboBox MakeSlotCombo() {
			return new ComboBox {
				DropDownStyle = ComboBoxStyle.DropDownList,
				Dock = DockStyle.Fill,
				Margin = new Padding(2),
				DropDownWidth = 400
			};
		}

		private void PopulateRowCombo(DataRowControls row, Slot slot, CustomSet model) {
			CustomSequence? current = this.Current();
			if (current == null || !current.Level.HasValue) {
				return;
			}

			LevelCatalog catalog = LevelCatalog.Get(current.Level.Value);
			List<object> items = SetEditor.BuildOptions(catalog, slot, model);

			ComboBox combo = row.GetCombo(slot);
			combo.BeginUpdate();
			combo.Items.Clear();
			foreach (object item in items) {
				combo.Items.Add(item);
			}

			int? selected = model.GetSlot(slot);
			combo.SelectedIndex = 0;
			if (selected.HasValue) {
				for (int i = 0; i < combo.Items.Count; i++) {
					if (combo.Items[i] is PieceOption opt && opt.Id == selected.Value) {
						combo.SelectedIndex = i;
						break;
					}
				}
			}

			combo.EndUpdate();
		}

		private static List<object> BuildOptions(LevelCatalog catalog, Slot slot, CustomSet model) {
			HashSet<int> usedEnemiesElsewhere = new();
			foreach (Slot other in new[] { Slot.P1, Slot.P2, Slot.P3 }) {
				if (other == slot) {
					continue;
				}

				int? id = model.GetSlot(other);
				if (id.HasValue && catalog.Enemies.Contains(id.Value)) {
					usedEnemiesElsewhere.Add(id.Value);
				}
			}

			IReadOnlyList<int> ownIds = slot switch {
				Slot.P1 => catalog.P1,
				Slot.P2 => catalog.P2,
				Slot.P3 => catalog.P3,
				_ => Array.Empty<int>()
			};

			List<PieceOption> pieces = new();
			foreach (int id in ownIds) {
				pieces.Add(new PieceOption(id, catalog.HintFor(id)));
			}

			foreach (int id in catalog.Enemies) {
				if (usedEnemiesElsewhere.Contains(id)) {
					continue;
				}

				pieces.Add(new PieceOption(id, catalog.HintFor(id)));
			}

			pieces.Sort((a, b) => string.Compare(a.Hint, b.Hint, StringComparison.InvariantCultureIgnoreCase));

			List<object> items = new();
			items.Add(SetEditor.None);
			foreach (PieceOption piece in pieces) {
				items.Add(piece);
			}

			return items;
		}

		private void OnRowComboChanged(DataRowControls row, Slot slot) {
			if (this.suspendEvents) {
				return;
			}

			CustomSequence? current = this.Current();
			if (current == null) {
				return;
			}

			int rowIndex = this.dataRows.IndexOf(row);
			if (rowIndex < 0 || rowIndex >= current.Sets.Count) {
				return;
			}

			CustomSet model = current.Sets[rowIndex];
			ComboBox combo = row.GetCombo(slot);
			int? newId = combo.SelectedItem is PieceOption opt ? opt.Id : (int?)null;
			model.SetSlot(slot, newId);

			this.suspendEvents = true;
			try {
				foreach (Slot other in new[] { Slot.P1, Slot.P2, Slot.P3 }) {
					if (other == slot) {
						continue;
					}

					this.PopulateRowCombo(row, other, model);
				}
			} finally {
				this.suspendEvents = false;
			}

			if (rowIndex == current.Sets.Count - 1 && !model.IsEmpty) {
				CustomSet trailing = new();
				current.Sets.Add(trailing);
				this.AddDataRow(trailing);
			}

			this.TryAutoSave();
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
			foreach (Slot slot in new[] { Slot.P1, Slot.P2, Slot.P3 }) {
				int? id = set.GetSlot(slot);
				string slotLabel = SetEditor.SlotLabel(slot);
				if (!id.HasValue) {
					errors.Add($"{seqLabel} row {rowNum}: {slotLabel} is not set.");
					continue;
				}

				if (!SetEditor.IsValidForSlot(catalog, slot, id.Value)) {
					errors.Add($"{seqLabel} row {rowNum}: {slotLabel} has id 0x{id.Value:X4} which is not a valid {slotLabel} or Enemy id.");
				}
			}

			HashSet<int> seen = new();
			HashSet<int> reported = new();
			foreach (Slot slot in new[] { Slot.P1, Slot.P2, Slot.P3 }) {
				int? id = set.GetSlot(slot);
				if (!id.HasValue) {
					continue;
				}

				if (!seen.Add(id.Value) && reported.Add(id.Value)) {
					errors.Add($"{seqLabel} row {rowNum}: piece id 0x{id.Value:X4} appears more than once.");
				}
			}
		}

		private static string SlotLabel(Slot slot) {
			return slot switch {
				Slot.P1 => "Piece 1",
				Slot.P2 => "Piece 2",
				Slot.P3 => "Piece 3",
				_ => slot.ToString()
			};
		}

		private static bool IsValidForSlot(LevelCatalog catalog, Slot slot, int id) {
			IReadOnlyList<int> ownIds = slot switch {
				Slot.P1 => catalog.P1,
				Slot.P2 => catalog.P2,
				Slot.P3 => catalog.P3,
				_ => Array.Empty<int>()
			};

			return ownIds.Contains(id) || catalog.Enemies.Contains(id);
		}

		private void OnRowDeleteClicked(DataRowControls row) {
			CustomSequence? current = this.Current();
			if (current == null) {
				return;
			}

			int rowIndex = this.dataRows.IndexOf(row);
			if (rowIndex < 0 || rowIndex >= current.Sets.Count) {
				return;
			}

			CustomSet model = current.Sets[rowIndex];
			if (!model.IsEmpty) {
				DialogResult choice = MessageBox.Show(
					this,
					"Delete this row?",
					"Delete Row",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning
				);

				if (choice != DialogResult.Yes) {
					return;
				}
			}

			current.Sets.RemoveAt(rowIndex);
			this.LoadSequenceIntoUi(current);
			this.TryAutoSave();
		}
	}
}
