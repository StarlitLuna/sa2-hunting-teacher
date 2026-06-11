namespace sa2_hunting_teacher.Updates;

partial class UpdateForm {
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpdateForm));
		splitContainer1 = new SplitContainer();
		changeLog = new RichTextBox();
		infoLabel1 = new Label();
		infoLabel2 = new Label();
		spinnerIcon = new PictureBox();
		yesBtn = new Button();
		noBtn = new Button();
		((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
		splitContainer1.Panel1.SuspendLayout();
		splitContainer1.Panel2.SuspendLayout();
		splitContainer1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)spinnerIcon).BeginInit();
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
		splitContainer1.Panel1.Controls.Add(changeLog);
		splitContainer1.Panel1.Controls.Add(infoLabel1);
		splitContainer1.Panel1.Controls.Add(infoLabel2);
		splitContainer1.Panel1.Paint += SplitContainer1_Panel1_Paint;
		splitContainer1.Panel1MinSize = 0;
		// 
		// splitContainer1.Panel2
		// 
		splitContainer1.Panel2.BackColor = SystemColors.ControlLight;
		splitContainer1.Panel2.Controls.Add(spinnerIcon);
		splitContainer1.Panel2.Controls.Add(yesBtn);
		splitContainer1.Panel2.Controls.Add(noBtn);
		splitContainer1.Panel2MinSize = 0;
		splitContainer1.Size = new Size(588, 339);
		splitContainer1.SplitterDistance = 268;
		splitContainer1.SplitterWidth = 1;
		splitContainer1.TabIndex = 0;
		// 
		// changeLog
		// 
		changeLog.Location = new Point(87, 53);
		changeLog.Name = "changeLog";
		changeLog.ReadOnly = true;
		changeLog.Size = new Size(485, 156);
		changeLog.TabIndex = 3;
		changeLog.Text = "";
		// 
		// infoLabel1
		// 
		infoLabel1.AutoSize = true;
		infoLabel1.Location = new Point(16, 18);
		infoLabel1.Name = "infoLabel1";
		infoLabel1.Size = new Size(180, 20);
		infoLabel1.TabIndex = 0;
		infoLabel1.Text = "A new version was found: ";
		// 
		// infoLabel2
		// 
		infoLabel2.AutoSize = true;
		infoLabel2.Location = new Point(16, 218);
		infoLabel2.Name = "infoLabel2";
		infoLabel2.Size = new Size(215, 20);
		infoLabel2.TabIndex = 2;
		infoLabel2.Text = "Would you like to update now?";
		// 
		// spinnerIcon
		// 
		spinnerIcon.BackColor = SystemColors.ButtonHighlight;
		spinnerIcon.Image = (Image)resources.GetObject("spinnerIcon.Image");
		spinnerIcon.InitialImage = null;
		spinnerIcon.Location = new Point(377, 24);
		spinnerIcon.Name = "spinnerIcon";
		spinnerIcon.Size = new Size(20, 20);
		spinnerIcon.TabIndex = 2;
		spinnerIcon.TabStop = false;
		spinnerIcon.Visible = false;
		// 
		// yesBtn
		// 
		yesBtn.Location = new Point(367, 20);
		yesBtn.Name = "yesBtn";
		yesBtn.Size = new Size(94, 29);
		yesBtn.TabIndex = 1;
		yesBtn.Text = "Yes";
		yesBtn.UseVisualStyleBackColor = true;
		yesBtn.Click += YesBtn_Click;
		// 
		// noBtn
		// 
		noBtn.Location = new Point(467, 20);
		noBtn.Name = "noBtn";
		noBtn.Size = new Size(94, 29);
		noBtn.TabIndex = 0;
		noBtn.Text = "No";
		noBtn.UseVisualStyleBackColor = true;
		noBtn.Click += NoBtn_Click;
		// 
		// UpdateForm
		// 
		AutoScaleDimensions = new SizeF(8F, 20F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(588, 339);
		ControlBox = false;
		Controls.Add(splitContainer1);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "UpdateForm";
		ShowIcon = false;
		ShowInTaskbar = false;
		SizeGripStyle = SizeGripStyle.Hide;
		StartPosition = FormStartPosition.CenterParent;
		Text = "Update Found";
		TopMost = true;
		splitContainer1.Panel1.ResumeLayout(false);
		splitContainer1.Panel1.PerformLayout();
		splitContainer1.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
		splitContainer1.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)spinnerIcon).EndInit();
		ResumeLayout(false);
	}

	#endregion

	private SplitContainer splitContainer1;
	private Label infoLabel1;
	private Label infoLabel2;
	private Button noBtn;
	private Button yesBtn;
	private PictureBox spinnerIcon;
	private RichTextBox changeLog;
}