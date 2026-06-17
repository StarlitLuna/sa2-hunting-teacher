using SharpCompress.Common;
using System.Collections;

namespace sa2_hunting_teacher {
	public partial class SetImporter : Form {
		private SetEditor editor;

		public SetImporter(SetEditor editor) {
			this.editor = editor;
			InitializeComponent();
		}

		private bool ValidateSets() {
			int count = 0;
			string[] sets = this.setsTextBox.Lines;

			foreach (string setRaw in sets) {
				string set = setRaw.Trim();
				if (set == "") {
					continue;
				}

				if (!set.All(char.IsDigit)) {
					this.ValidationError("The set: \"" + set + "\" is invalid!");
					return false;
				}

				int setVal = Convert.ToInt32(set);
				if (setVal < 0) {
					this.ValidationError("The set: \"" + set + "\" is below the min set frame value of 0!");
					return false;
				}

				if (setVal > 1023) {
					this.ValidationError("The set: \"" + set + "\" is above the max set frame value of 1023!");
					return false;
				}

				count++;
			}

			if (count <= 0) {
				this.ValidationError("You did not enter any sets!");
				return false;
			}

			return true;
		}

		private void ValidationError(string msg) {
			MessageBox.Show(this, msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void importBtn_Click(object sender, EventArgs e) {
			if (!this.ValidateSets()) {
				return;
			}

			List<int> sets = [];
			foreach (string setRaw in this.setsTextBox.Lines) {
				string set = setRaw.Trim();
				if (set == "") {
					continue;
				}

				sets.Add(Convert.ToInt32(set));
			}

			this.editor.importSets([..sets], this.storyStyle.Checked);
			this.Dispose();
		}
	}
}
