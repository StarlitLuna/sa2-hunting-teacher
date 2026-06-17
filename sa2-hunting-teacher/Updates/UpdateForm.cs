using MarkdownToRtf;

namespace sa2_hunting_teacher.Updates {
	public partial class UpdateForm : Form {
		private UpdateManager updateManager;
		private Release release;

		public UpdateForm(UpdateManager updateManager, Release release) {
			InitializeComponent();

			this.updateManager = updateManager;
			this.release = release;
			this.Text += " - " + release.TagName;
			this.infoLabel1.Text += release.TagName;
			this.infoLabel1.Location = new Point(18 + this.infoLabel1.Location.X + SystemIcons.Information.Width, this.infoLabel1.Location.Y);
			this.infoLabel2.Location = new Point(18 + this.infoLabel2.Location.X + SystemIcons.Information.Width, this.infoLabel2.Location.Y);
			this.changeLog.Rtf = MarkdownToRtfConverter.Convert(release.Body ?? "");
		}

		private void SplitContainer1_Panel1_Paint(object sender, PaintEventArgs e) {
			Icon icon = SystemIcons.Information;
			e.Graphics.DrawIconUnstretched(icon, new Rectangle(18, 18, icon.Width, icon.Height));
		}

		private void YesBtn_Click(object sender, EventArgs e) {
			this.yesBtn.Enabled = false;
			this.noBtn.Enabled = false;
			this.spinnerIcon.Visible = true;

			Task.Run(async () => {
				await this.updateManager.PerformUpdate(this, this.release);
				this.Invoke(this.Close);
			});
		}

		private void NoBtn_Click(object sender, EventArgs e) {
			this.Close();
		}
	}
}
