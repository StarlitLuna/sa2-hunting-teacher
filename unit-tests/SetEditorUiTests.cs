using sa2_hunting_teacher;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Windows.Forms;

namespace unit_tests;

[Collection(StaticStateCollection.Name)]
public class SetEditorUiTests : IDisposable {
	private static readonly Type SetEditorType = typeof(SetEditor);
	private static readonly Type SlotType =
		SetEditorType.GetNestedType("Slot", BindingFlags.NonPublic)!;

	private readonly bool appDataDirExisted;
	private readonly bool settingsFileExisted;
	private readonly byte[]? backupContent;

	public SetEditorUiTests() {
		this.appDataDirExisted = Directory.Exists(Settings.AppDataPath);
		this.settingsFileExisted = File.Exists(Settings.SettingsPath);
		if (this.settingsFileExisted) {
			this.backupContent = File.ReadAllBytes(Settings.SettingsPath);
		}

		if (File.Exists(Settings.SettingsPath)) {
			File.Delete(Settings.SettingsPath);
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

	[Fact]
	public void RowDelete_EmptyTrailingRow_DoesNotRebuildExistingRows() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditorWithOneValidSequence();
			SelectSequence(editor, 0);
			IList rows = DataRows(editor);
			Assert.Equal(2, rows.Count);
			ComboBox originalP1 = RowCombo(rows[0]!, "P1");

			Reflect.Invoke(editor, "OnRowDeleteClicked", rows[1]);

			IList updatedRows = DataRows(editor);
			Assert.Equal(2, updatedRows.Count);
			Assert.Same(originalP1, RowCombo(updatedRows[0]!, "P1"));
		});
	}

	[Fact]
	public void RowEdit_SchedulesAutosaveWithoutImmediateDiskWrite_AndFlushesOnClose() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditorWithOneValidSequence();
			SelectSequence(editor, 0);
			IList rows = DataRows(editor);
			object row = rows[0]!;
			ComboBox p1 = RowCombo(row, "P1");
			int newP1 = LevelCatalog.Get(Level.AquaticMine).P1.Skip(1).First();

			InvokeComboDropDown(editor, row, "P1");
			SelectPiece(p1, newP1);

			Assert.False(File.Exists(Settings.SettingsPath));

			editor.Close();

			Settings loaded = Settings.Load();
			Assert.Single(loaded.CustomSequences);
			Assert.Equal(newP1, loaded.CustomSequences[0].Sets[0].P1Id);
		});
	}

	[Fact]
	public void ExplicitSave_FlushesPendingAutosaveImmediately() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditorWithOneValidSequence();
			SelectSequence(editor, 0);
			IList rows = DataRows(editor);
			object row = rows[0]!;
			int newP2 = LevelCatalog.Get(Level.AquaticMine).P2.Skip(1).First();
			InvokeComboDropDown(editor, row, "P2");
			SelectPiece(RowCombo(row, "P2"), newP2);

			Reflect.Invoke(
				editor,
				"setEditorSave_Click",
				Reflect.GetField<Button>(editor, "setEditorSave"),
				EventArgs.Empty
			);

			Settings loaded = Settings.Load();
			Assert.Single(loaded.CustomSequences);
			Assert.Equal(newP2, loaded.CustomSequences[0].Sets[0].P2Id);
		});
	}

	[Fact]
	public void ExplicitSave_WithPendingAutosaveWritesSettingsOnce() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditorWithOneValidSequence();
			SelectSequence(editor, 0);
			IList rows = DataRows(editor);
			object row = rows[0]!;
			InvokeComboDropDown(editor, row, "P2");
			SelectPiece(RowCombo(row, "P2"), LevelCatalog.Get(Level.AquaticMine).P2.Skip(1).First());

			int changedEvents = CountSettingsChangedEvents(() =>
				Reflect.Invoke(
					editor,
					"setEditorSave_Click",
					Reflect.GetField<Button>(editor, "setEditorSave"),
					EventArgs.Empty
				)
			);

			Assert.Equal(1, changedEvents);
		});
	}

	[Fact]
	public void ImportSetsButton_DisabledWhenNoSequenceSelected() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditorWithOneValidSequence();
			Button importSetsBtn = Reflect.GetField<Button>(editor, "importSetsBtn");

			Assert.False(importSetsBtn.Enabled);
		});
	}

	[Fact]
	public void ImportSetsButton_EnabledWhenSequenceSelected() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditorWithOneValidSequence();
			Button importSetsBtn = Reflect.GetField<Button>(editor, "importSetsBtn");

			SelectSequence(editor, 0);

			Assert.True(importSetsBtn.Enabled);
		});
	}

	[Fact]
	public void ImportSetsButton_IsPlacedNextToSaveButton() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditor(SettingsWithSequence());
			Button importSetsBtn = Reflect.GetField<Button>(editor, "importSetsBtn");
			Button setEditorSave = Reflect.GetField<Button>(editor, "setEditorSave");

			Assert.Equal("Import Sets", importSetsBtn.Text);
			Assert.True(importSetsBtn.Right <= setEditorSave.Left);
			Assert.Equal(setEditorSave.Top, importSetsBtn.Top);
		});
	}

	[Fact]
	public void ImportSetsButton_ClickOpensSetImporterDialog() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditor(SettingsWithSequence());
			Button importSetsBtn = Reflect.GetField<Button>(editor, "importSetsBtn");
			bool sawImporter = false;

			using System.Windows.Forms.Timer closeTimer = new() {
				Interval = 25
			};
			closeTimer.Tick += (_, _) => {
				foreach (Form form in Application.OpenForms.Cast<Form>().ToArray()) {
					if (form is SetImporter importer) {
						sawImporter = true;
						importer.Close();
					}
				}
			};

			closeTimer.Start();
			Reflect.Invoke(editor, "importSetsBtn_Click", importSetsBtn, EventArgs.Empty);
			closeTimer.Stop();

			Assert.True(sawImporter);
		});
	}

	[Fact]
	public void InvalidSequence_DoesNotAutosaveOnClose() {
		StaHelper.RunSta(() => {
			Settings settings = SettingsWithSequence(
				new HuntingSequence {
					Id = 1,
					Name = "Broken",
					Level = Level.AquaticMine,
					Sets = [
						new HuntingSet {
							P1Id = LevelCatalog.Get(Level.AquaticMine).P1.First(),
							P2Id = LevelCatalog.Get(Level.AquaticMine).P2.First(),
							P3Id = LevelCatalog.Get(Level.AquaticMine).P3.First()
						}
					]
				}
			);
			using SetEditor editor = BuildEditor(settings);
			SelectSequence(editor, 0);
			SelectPiece(RowCombo(DataRows(editor)[0]!, "P2"), null);

			editor.Close();

			Assert.False(File.Exists(Settings.SettingsPath));
		});
	}

	[Fact]
	public void AddSequence_SelectsNewSequenceWithoutDuplicatingLoadedRows() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditor(SettingsWithSequence());

			Reflect.Invoke(
				editor,
				"addSequence_Click",
				Reflect.GetField<Button>(editor, "addSequence"),
				EventArgs.Empty
			);
			ComboBox levelSelector = Reflect.GetField<ComboBox>(editor, "setEditorLevels");
			levelSelector.SelectedValue = Level.AquaticMine;

			IList rows = DataRows(editor);
			Assert.Single(rows);
			Assert.True(RowModel(rows[0]!).GetType().GetProperty("IsEmpty")!.GetValue(RowModel(rows[0]!)) is true);
		});
	}

	[Fact]
	public void SequenceSelection_LoadsRowsWithCollapsedComboOptions() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditorWithOneValidSequence();
			SelectSequence(editor, 0);

			IList rows = DataRows(editor);
			Assert.Equal(2, rows.Count);
			Assert.Equal(2, RowCombo(rows[0]!, "P1").Items.Count);
			Assert.Equal(2, RowCombo(rows[0]!, "P2").Items.Count);
			Assert.Equal(2, RowCombo(rows[0]!, "P3").Items.Count);
			Assert.Single(RowCombo(rows[1]!, "P1").Items);
			Assert.Single(RowCombo(rows[1]!, "P2").Items);
			Assert.Single(RowCombo(rows[1]!, "P3").Items);
		});
	}

	[Fact]
	public void DropdownOpen_ExpandsCollapsedComboAndPreservesSelection() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditorWithOneValidSequence();
			SelectSequence(editor, 0);
			object row = DataRows(editor)[0]!;
			ComboBox p1 = RowCombo(row, "P1");
			object selectedBefore = p1.SelectedItem!;

			InvokeComboDropDown(editor, row, "P1");

			Assert.True(p1.Items.Count > 2);
			Assert.Same(selectedBefore, p1.SelectedItem);
			Assert.Equal(LevelCatalog.Get(Level.AquaticMine).P1.Count + LevelCatalog.Get(Level.AquaticMine).Enemies.Count + 1, p1.Items.Count);
		});
	}

	[Fact]
	public void AddingTrailingRowAfterFirstPieceSelection_KeepsNewRowCollapsed() {
		StaHelper.RunSta(() => {
			using SetEditor editor = BuildEditorWithOneValidSequence();
			SelectSequence(editor, 0);
			IList rows = DataRows(editor);
			object trailing = rows[1]!;
			ComboBox p1 = RowCombo(trailing, "P1");
			InvokeComboDropDown(editor, trailing, "P1");
			SelectPiece(p1, LevelCatalog.Get(Level.AquaticMine).P1.First());

			IList updatedRows = DataRows(editor);
			Assert.Equal(3, updatedRows.Count);
			object newTrailing = updatedRows[2]!;
			Assert.Single(RowCombo(newTrailing, "P1").Items);
			Assert.Single(RowCombo(newTrailing, "P2").Items);
			Assert.Single(RowCombo(newTrailing, "P3").Items);
		});
	}

	private static SetEditor BuildEditorWithOneValidSequence() {
		LevelCatalog catalog = LevelCatalog.Get(Level.AquaticMine);
		Settings settings = SettingsWithSequence(
			new HuntingSequence {
				Id = 1,
				Name = "Practice",
				Level = Level.AquaticMine,
				Sets = [
					new HuntingSet {
						P1Id = catalog.P1.First(),
						P2Id = catalog.P2.First(),
						P3Id = catalog.P3.First()
					}
				]
			}
		);

		return BuildEditor(settings);
	}

	private static SetEditor BuildEditor(Settings settings) {
		SetEditor editor = new(settings, []);
		_ = editor.Handle;
		_ = Reflect.GetField<ListView>(editor, "customSequences").Handle;
		_ = Reflect.GetField<TableLayoutPanel>(editor, "tableLayoutPanel1").Handle;
		return editor;
	}

	private static Settings SettingsWithSequence(params HuntingSequence[] sequences) {
#pragma warning disable CS0618
		Settings settings = new() {
			NextSequenceId = sequences.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1
		};
#pragma warning restore CS0618
		settings.CustomSequences.AddRange(sequences);
		return settings;
	}

	private static void SelectSequence(SetEditor editor, int index) {
		ListView list = Reflect.GetField<ListView>(editor, "customSequences");
		list.Items[index].Selected = true;
	}

	private static int CountSettingsChangedEvents(Action action) {
		Directory.CreateDirectory(Settings.AppDataPath);
		ConcurrentQueue<string> events = new();
		using FileSystemWatcher watcher = new(Settings.AppDataPath, Path.GetFileName(Settings.SettingsPath)) {
			NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
		};
		FileSystemEventHandler handler = (_, _) => events.Enqueue("changed");
		watcher.Changed += handler;
		watcher.EnableRaisingEvents = true;

		action();
		Thread.Sleep(250);

		watcher.EnableRaisingEvents = false;
		return events.Count;
	}

	private static void InvokeComboDropDown(SetEditor editor, object row, string slotName) {
		object slot = Enum.Parse(SlotType, slotName);
		Reflect.Invoke(editor, "OnRowComboDropDown", row, slot);
	}

	private static IList DataRows(SetEditor editor) {
		return Reflect.GetField<IList>(editor, "dataRows");
	}

	private static ComboBox RowCombo(object row, string propertyName) {
		return (ComboBox)row.GetType().GetProperty(propertyName)!.GetValue(row)!;
	}

	private static object RowModel(object row) {
		return row.GetType().GetProperty("Model")!.GetValue(row)!;
	}

	private static void SelectPiece(ComboBox combo, int? id) {
		if (!id.HasValue) {
			combo.SelectedIndex = 0;
			return;
		}

		for (int i = 0; i < combo.Items.Count; i++) {
			object item = combo.Items[i]!;
			PropertyInfo? idProperty = item.GetType().GetProperty("Id");
			if (idProperty != null && (int)idProperty.GetValue(item)! == id.Value) {
				combo.SelectedIndex = i;
				return;
			}
		}

		throw new ArgumentException($"Piece id 0x{id.Value:X4} is not present in the combo.");
	}
}
