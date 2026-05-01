using sa2_hunting_teacher.DropdownControls;

namespace sa2_hunting_teacher;

partial class HuntingTeacherForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

	#region Windows Form Designer generated code

	/// <summary>
	///  Required method for Designer support - do not modify
	///  the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		components = new System.ComponentModel.Container();
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HuntingTeacherForm));
		panel1 = new Panel();
		mspHints = new ComboBox();
		setEditor = new Button();
		timerReset = new CheckBox();
		inPlaceRepetitions = new CheckBox();
		label3 = new Label();
		backToMenu = new CheckBox();
		label2 = new Label();
		repetitions = new NumericUpDown();
		resetBtn = new Button();
		startBtn = new Button();
		label1 = new Label();
		levelSelector = new GroupedComboBox();
		panel2 = new Panel();
		logBox = new TextBox();
		backToMenuTooltip = new ToolTip(components);
		inPlaceRepetitionsTooltip = new ToolTip(components);
		timerResetTooltip = new ToolTip(components);
		mspHintsTooltip = new ToolTip(components);
		mspHintsLabel = new Label();
		panel1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)repetitions).BeginInit();
		panel2.SuspendLayout();
		SuspendLayout();
		// 
		// panel1
		// 
		panel1.Controls.Add(mspHintsLabel);
		panel1.Controls.Add(mspHints);
		panel1.Controls.Add(setEditor);
		panel1.Controls.Add(timerReset);
		panel1.Controls.Add(inPlaceRepetitions);
		panel1.Controls.Add(label3);
		panel1.Controls.Add(backToMenu);
		panel1.Controls.Add(label2);
		panel1.Controls.Add(repetitions);
		panel1.Controls.Add(resetBtn);
		panel1.Controls.Add(startBtn);
		panel1.Controls.Add(label1);
		panel1.Controls.Add(levelSelector);
		panel1.Dock = DockStyle.Top;
		panel1.Location = new Point(0, 0);
		panel1.Name = "panel1";
		panel1.Size = new Size(855, 80);
		panel1.TabIndex = 0;
		// 
		// mspHints
		// 
		mspHints.DropDownStyle = ComboBoxStyle.DropDownList;
		mspHints.FormattingEnabled = true;
		mspHints.Location = new Point(169, 48);
		mspHints.Name = "mspHints";
		mspHints.Size = new Size(151, 28);
		mspHints.TabIndex = 12;
		mspHints.SelectedIndexChanged += mspHints_SelectedIndexChanged;
		// 
		// setEditor
		// 
		setEditor.Location = new Point(359, 13);
		setEditor.Name = "setEditor";
		setEditor.Size = new Size(94, 29);
		setEditor.TabIndex = 11;
		setEditor.Text = "Set Editor";
		setEditor.UseVisualStyleBackColor = true;
		setEditor.Click += setEditor_Click;
		// 
		// timerReset
		// 
		timerReset.AutoSize = true;
		timerReset.Checked = true;
		timerReset.CheckState = CheckState.Checked;
		timerReset.Location = new Point(455, 50);
		timerReset.Name = "timerReset";
		timerReset.Size = new Size(109, 24);
		timerReset.TabIndex = 10;
		timerReset.Text = "Timer Reset";
		timerReset.UseVisualStyleBackColor = true;
		timerReset.CheckedChanged += SettingsChanged;
		// 
		// inPlaceRepetitions
		// 
		inPlaceRepetitions.AutoSize = true;
		inPlaceRepetitions.Location = new Point(570, 50);
		inPlaceRepetitions.Name = "inPlaceRepetitions";
		inPlaceRepetitions.Size = new Size(161, 24);
		inPlaceRepetitions.TabIndex = 9;
		inPlaceRepetitions.Text = "Repetitions In Place";
		inPlaceRepetitions.UseVisualStyleBackColor = true;
		inPlaceRepetitions.CheckedChanged += SettingsChanged;
		// 
		// label3
		// 
		label3.AutoSize = true;
		label3.Location = new Point(12, 51);
		label3.Name = "label3";
		label3.Size = new Size(65, 20);
		label3.TabIndex = 8;
		label3.Text = "Settings:";
		// 
		// backToMenu
		// 
		backToMenu.AutoSize = true;
		backToMenu.Location = new Point(326, 50);
		backToMenu.Name = "backToMenu";
		backToMenu.Size = new Size(123, 24);
		backToMenu.TabIndex = 7;
		backToMenu.Text = "Back To Menu";
		backToMenu.UseVisualStyleBackColor = true;
		backToMenu.CheckedChanged += SettingsChanged;
		// 
		// label2
		// 
		label2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		label2.AutoSize = true;
		label2.Location = new Point(505, 17);
		label2.Name = "label2";
		label2.Size = new Size(87, 20);
		label2.TabIndex = 5;
		label2.Text = "Repetitions:";
		// 
		// repetitions
		// 
		repetitions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		repetitions.Location = new Point(600, 15);
		repetitions.Maximum = new decimal(new int[] { 9, 0, 0, 0 });
		repetitions.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
		repetitions.Name = "repetitions";
		repetitions.Size = new Size(43, 27);
		repetitions.TabIndex = 4;
		repetitions.Value = new decimal(new int[] { 3, 0, 0, 0 });
		repetitions.ValueChanged += SettingsChanged;
		// 
		// resetBtn
		// 
		resetBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		resetBtn.Enabled = false;
		resetBtn.Location = new Point(649, 13);
		resetBtn.Name = "resetBtn";
		resetBtn.Size = new Size(94, 29);
		resetBtn.TabIndex = 3;
		resetBtn.Text = "Reset";
		resetBtn.UseVisualStyleBackColor = true;
		resetBtn.Click += ResetBtn_Click;
		// 
		// startBtn
		// 
		startBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		startBtn.Location = new Point(749, 12);
		startBtn.Name = "startBtn";
		startBtn.Size = new Size(94, 29);
		startBtn.TabIndex = 2;
		startBtn.Text = "Start";
		startBtn.UseVisualStyleBackColor = true;
		startBtn.Click += StartBtn_Click;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Location = new Point(12, 16);
		label1.Name = "label1";
		label1.Size = new Size(90, 20);
		label1.TabIndex = 1;
		label1.Text = "Level Select:";
		// 
		// levelSelector
		// 
		levelSelector.DataSource = null;
		levelSelector.DropDownStyle = ComboBoxStyle.DropDownList;
		levelSelector.FormattingEnabled = true;
		levelSelector.Location = new Point(108, 14);
		levelSelector.Name = "levelSelector";
		levelSelector.Size = new Size(245, 28);
		levelSelector.TabIndex = 0;
		// 
		// panel2
		// 
		panel2.Controls.Add(logBox);
		panel2.Dock = DockStyle.Fill;
		panel2.Location = new Point(0, 80);
		panel2.Name = "panel2";
		panel2.Size = new Size(855, 396);
		panel2.TabIndex = 1;
		// 
		// logBox
		// 
		logBox.Dock = DockStyle.Fill;
		logBox.Location = new Point(0, 0);
		logBox.Multiline = true;
		logBox.Name = "logBox";
		logBox.ReadOnly = true;
		logBox.ScrollBars = ScrollBars.Vertical;
		logBox.Size = new Size(855, 396);
		logBox.TabIndex = 0;
		logBox.WordWrap = false;
		// 
		// mspHintsLabel
		// 
		mspHintsLabel.AutoSize = true;
		mspHintsLabel.Location = new Point(83, 51);
		mspHintsLabel.Name = "mspHintsLabel";
		mspHintsLabel.Size = new Size(80, 20);
		mspHintsLabel.TabIndex = 13;
		mspHintsLabel.Text = "MSp Hints:";
		// 
		// HuntingTeacherForm
		// 
		AutoScaleDimensions = new SizeF(8F, 20F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(855, 476);
		Controls.Add(panel2);
		Controls.Add(panel1);
		Icon = (Icon)resources.GetObject("$this.Icon");
		MinimumSize = new Size(835, 400);
		Name = "HuntingTeacherForm";
		Text = "Hunting Teacher";
		panel1.ResumeLayout(false);
		panel1.PerformLayout();
		((System.ComponentModel.ISupportInitialize)repetitions).EndInit();
		panel2.ResumeLayout(false);
		panel2.PerformLayout();
		ResumeLayout(false);
	}

	#endregion

	private Panel panel1;
	private Panel panel2;
	private Label label1;
	private Button resetBtn;
	private Button startBtn;
	private TextBox logBox;
	private NumericUpDown repetitions;
	private Label label2;
	private GroupedComboBox levelSelector;
	private Label label3;
	private CheckBox backToMenu;
	private ToolTip backToMenuTooltip;
	private CheckBox inPlaceRepetitions;
	private ToolTip inPlaceRepetitionsTooltip;
	private CheckBox timerReset;
	private ToolTip timerResetTooltip;
	private Button setEditor;
	private ComboBox mspHints;
	private ToolTip mspHintsTooltip;
	private Label mspHintsLabel;
}
