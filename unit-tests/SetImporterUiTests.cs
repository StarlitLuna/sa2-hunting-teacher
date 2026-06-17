using sa2_hunting_teacher;
using System.Windows.Forms;

namespace unit_tests;

public class SetImporterUiTests {
	[Fact]
	public void Constructor_BuildsImportShell() {
		StaHelper.RunSta(() => {
			using SetImporter importer = new();
			_ = importer.Handle;

			SplitContainer splitContainer1 = Reflect.GetField<SplitContainer>(importer, "splitContainer1");
			TableLayoutPanel bottomControls = Reflect.GetField<TableLayoutPanel>(importer, "bottomControls");
			TextBox setsTextBox = Reflect.GetField<TextBox>(importer, "setsTextBox");
			Button importBtn = Reflect.GetField<Button>(importer, "importBtn");
			CheckBox storyStyle = Reflect.GetField<CheckBox>(importer, "storyStyle");

			Assert.Equal("Import Sets", importer.Text);
			Assert.Equal(DockStyle.Fill, splitContainer1.Dock);
			Assert.Equal(Orientation.Horizontal, splitContainer1.Orientation);
			Assert.Same(setsTextBox, splitContainer1.Panel1.Controls[0]);
			Assert.Equal(DockStyle.Fill, setsTextBox.Dock);
			Assert.True(setsTextBox.Multiline);
			Assert.Same(bottomControls, splitContainer1.Panel2.Controls[0]);
			Assert.Equal(DockStyle.Fill, bottomControls.Dock);
			Assert.Same(storyStyle, bottomControls.GetControlFromPosition(0, 0));
			Assert.Same(importBtn, bottomControls.GetControlFromPosition(1, 0));
			Assert.Equal("Import", importBtn.Text);
			Assert.Equal("Story Style", storyStyle.Text);
			Assert.True(importBtn.Right <= bottomControls.ClientSize.Width);
		});
	}
}
