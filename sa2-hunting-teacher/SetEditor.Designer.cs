namespace sa2_hunting_teacher {
	partial class SetEditor {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetEditor));
			splitContainer1 = new SplitContainer();
			addSequence = new Button();
			splitContainer2 = new SplitContainer();
			customSequences = new ListBox();
			tableLayoutPanel1 = new TableLayoutPanel();
			p3Label = new Label();
			p2Label = new Label();
			p1Label = new Label();
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
			splitContainer2.Panel1.SuspendLayout();
			splitContainer2.Panel2.SuspendLayout();
			splitContainer2.SuspendLayout();
			tableLayoutPanel1.SuspendLayout();
			SuspendLayout();
			// 
			// splitContainer1
			// 
			splitContainer1.Dock = DockStyle.Fill;
			splitContainer1.Location = new Point(0, 0);
			splitContainer1.Name = "splitContainer1";
			splitContainer1.Orientation = Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			splitContainer1.Panel1.Controls.Add(addSequence);
			// 
			// splitContainer1.Panel2
			// 
			splitContainer1.Panel2.Controls.Add(splitContainer2);
			splitContainer1.Size = new Size(873, 519);
			splitContainer1.SplitterDistance = 56;
			splitContainer1.TabIndex = 0;
			// 
			// addSequence
			// 
			addSequence.Location = new Point(12, 12);
			addSequence.Name = "addSequence";
			addSequence.Size = new Size(94, 29);
			addSequence.TabIndex = 0;
			addSequence.Text = "button1";
			addSequence.UseVisualStyleBackColor = true;
			addSequence.Click += addSequence_Click;
			// 
			// splitContainer2
			// 
			splitContainer2.Dock = DockStyle.Fill;
			splitContainer2.Location = new Point(0, 0);
			splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			splitContainer2.Panel1.Controls.Add(customSequences);
			// 
			// splitContainer2.Panel2
			// 
			splitContainer2.Panel2.AutoScroll = true;
			splitContainer2.Panel2.Controls.Add(tableLayoutPanel1);
			splitContainer2.Size = new Size(873, 459);
			splitContainer2.SplitterDistance = 180;
			splitContainer2.TabIndex = 0;
			// 
			// customSequences
			// 
			customSequences.BorderStyle = BorderStyle.FixedSingle;
			customSequences.Dock = DockStyle.Fill;
			customSequences.FormattingEnabled = true;
			customSequences.Location = new Point(0, 0);
			customSequences.Name = "customSequences";
			customSequences.Size = new Size(180, 459);
			customSequences.TabIndex = 0;
			// 
			// tableLayoutPanel1
			// 
			tableLayoutPanel1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			tableLayoutPanel1.CellBorderStyle = TableLayoutPanelCellBorderStyle.Inset;
			tableLayoutPanel1.ColumnCount = 3;
			tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45.91195F));
			tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54.08805F));
			tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 211F));
			tableLayoutPanel1.Controls.Add(p3Label, 2, 0);
			tableLayoutPanel1.Controls.Add(p2Label, 1, 0);
			tableLayoutPanel1.Controls.Add(p1Label, 0, 0);
			tableLayoutPanel1.Dock = DockStyle.Top;
			tableLayoutPanel1.Location = new Point(0, 0);
			tableLayoutPanel1.Name = "tableLayoutPanel1";
			tableLayoutPanel1.RowCount = 2;
			tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 58.44156F));
			tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 41.55844F));
			tableLayoutPanel1.Size = new Size(689, 79);
			tableLayoutPanel1.TabIndex = 0;
			// 
			// p3Label
			// 
			p3Label.AutoSize = true;
			p3Label.Dock = DockStyle.Fill;
			p3Label.Font = new Font("Segoe UI", 16F);
			p3Label.Location = new Point(478, 2);
			p3Label.Name = "p3Label";
			p3Label.Size = new Size(206, 42);
			p3Label.TabIndex = 2;
			p3Label.Text = "Piece 3";
			p3Label.TextAlign = ContentAlignment.MiddleCenter;
			// 
			// p2Label
			// 
			p2Label.AutoSize = true;
			p2Label.Dock = DockStyle.Fill;
			p2Label.Font = new Font("Segoe UI", 16F);
			p2Label.Location = new Point(222, 2);
			p2Label.Name = "p2Label";
			p2Label.Size = new Size(248, 42);
			p2Label.TabIndex = 1;
			p2Label.Text = "Piece 2";
			p2Label.TextAlign = ContentAlignment.MiddleCenter;
			// 
			// p1Label
			// 
			p1Label.AutoSize = true;
			p1Label.Dock = DockStyle.Fill;
			p1Label.Font = new Font("Segoe UI", 16F);
			p1Label.Location = new Point(5, 2);
			p1Label.Name = "p1Label";
			p1Label.Size = new Size(209, 42);
			p1Label.TabIndex = 0;
			p1Label.Text = "Piece 1";
			p1Label.TextAlign = ContentAlignment.MiddleCenter;
			// 
			// SetEditor
			// 
			AutoScaleDimensions = new SizeF(8F, 20F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(873, 519);
			Controls.Add(splitContainer1);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			Icon = (Icon)resources.GetObject("$this.Icon");
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "SetEditor";
			SizeGripStyle = SizeGripStyle.Hide;
			Text = "Set Editor";
			TopMost = true;
			splitContainer1.Panel1.ResumeLayout(false);
			splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
			splitContainer1.ResumeLayout(false);
			splitContainer2.Panel1.ResumeLayout(false);
			splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
			splitContainer2.ResumeLayout(false);
			tableLayoutPanel1.ResumeLayout(false);
			tableLayoutPanel1.PerformLayout();
			ResumeLayout(false);
		}

		#endregion

		private SplitContainer splitContainer1;
		private SplitContainer splitContainer2;
		private ListBox customSequences;
		private Button addSequence;
		private TableLayoutPanel tableLayoutPanel1;
		private Label p1Label;
		private Label p3Label;
		private Label p2Label;
	}
}