namespace sa2_hunting_teacher {
	partial class SetImporter {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			components = new System.ComponentModel.Container();
			splitContainer1 = new SplitContainer();
			setsTextBox = new TextBox();
			bottomControls = new TableLayoutPanel();
			storyStyle = new CheckBox();
			importBtn = new Button();
			storyStyleTooltip = new ToolTip(components);
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
			bottomControls.SuspendLayout();
			SuspendLayout();
			// 
			// splitContainer1
			// 
			splitContainer1.Dock = DockStyle.Fill;
			splitContainer1.IsSplitterFixed = true;
			splitContainer1.Location = new Point(0, 0);
			splitContainer1.Name = "splitContainer1";
			splitContainer1.Orientation = Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			splitContainer1.Panel1.Controls.Add(setsTextBox);
			splitContainer1.Panel1MinSize = 0;
			// 
			// splitContainer1.Panel2
			// 
			splitContainer1.Panel2.BackColor = SystemColors.ControlLight;
			splitContainer1.Panel2.Controls.Add(bottomControls);
			splitContainer1.Panel2MinSize = 0;
			splitContainer1.Size = new Size(640, 420);
			splitContainer1.SplitterDistance = 365;
			splitContainer1.SplitterWidth = 1;
			splitContainer1.TabIndex = 0;
			// 
			// setsTextBox
			// 
			setsTextBox.Dock = DockStyle.Fill;
			setsTextBox.Location = new Point(0, 0);
			setsTextBox.Multiline = true;
			setsTextBox.Name = "setsTextBox";
			setsTextBox.ScrollBars = ScrollBars.Both;
			setsTextBox.Size = new Size(640, 365);
			setsTextBox.TabIndex = 0;
			setsTextBox.WordWrap = false;
			// 
			// bottomControls
			// 
			bottomControls.ColumnCount = 2;
			bottomControls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			bottomControls.ColumnStyles.Add(new ColumnStyle());
			bottomControls.Controls.Add(storyStyle, 0, 0);
			bottomControls.Controls.Add(importBtn, 1, 0);
			bottomControls.Dock = DockStyle.Fill;
			bottomControls.Location = new Point(0, 0);
			bottomControls.Name = "bottomControls";
			bottomControls.RowCount = 1;
			bottomControls.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			bottomControls.Size = new Size(640, 54);
			bottomControls.TabIndex = 0;
			// 
			// storyStyle
			// 
			storyStyle.Anchor = AnchorStyles.Left;
			storyStyle.AutoSize = true;
			storyStyle.Cursor = Cursors.Help;
			storyStyle.Location = new Point(12, 15);
			storyStyle.Margin = new Padding(12, 3, 3, 3);
			storyStyle.Name = "storyStyle";
			storyStyle.Size = new Size(101, 24);
			storyStyle.TabIndex = 0;
			storyStyle.Text = "Story Style";
			storyStyleTooltip.SetToolTip(storyStyle, "For Pumpkin Hill and Egg Quarters:\r\nDetermines whether the set #s will use\r\nNG or NG+ pieces. Tick on for NG, leave off for NG+\r\nFor all other levels this does nothing.");
			storyStyle.UseVisualStyleBackColor = true;
			// 
			// importBtn
			// 
			importBtn.Anchor = AnchorStyles.Right;
			importBtn.Location = new Point(534, 12);
			importBtn.Margin = new Padding(3, 3, 12, 3);
			importBtn.Name = "importBtn";
			importBtn.Size = new Size(94, 29);
			importBtn.TabIndex = 1;
			importBtn.Text = "Import";
			importBtn.UseVisualStyleBackColor = true;
			importBtn.Click += importBtn_Click;
			// 
			// SetImporter
			// 
			AutoScaleDimensions = new SizeF(8F, 20F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(640, 420);
			Controls.Add(splitContainer1);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "SetImporter";
			ShowInTaskbar = false;
			SizeGripStyle = SizeGripStyle.Hide;
			StartPosition = FormStartPosition.CenterParent;
			Text = "Import Sets - One Set # Per Line";
			TopMost = true;
			splitContainer1.Panel1.ResumeLayout(false);
			splitContainer1.Panel1.PerformLayout();
			splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
			splitContainer1.ResumeLayout(false);
			bottomControls.ResumeLayout(false);
			bottomControls.PerformLayout();
			ResumeLayout(false);
		}

		#endregion

		private SplitContainer splitContainer1;
		private TextBox setsTextBox;
		private TableLayoutPanel bottomControls;
		private Button importBtn;
		private CheckBox storyStyle;
		private ToolTip storyStyleTooltip;
	}
}
