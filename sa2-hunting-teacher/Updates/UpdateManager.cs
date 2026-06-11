using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Factories;
using SharpCompress.Readers;
using System.Diagnostics;
using System.Security.Cryptography;

namespace sa2_hunting_teacher.Updates;

public class UpdateManager {
	private static readonly string API_URL = "https://api.github.com/repos/StarlitLuna/sa2-hunting-teacher";
	private static readonly string ASSET_NAME = "sa2-hunting-teacher.7z";

	private readonly HuntingTeacherForm mainForm;
	private readonly HttpClient client;

	private string? latestTag;

	public UpdateManager(HuntingTeacherForm mainForm) {
		this.mainForm = mainForm;

		this.client = new HttpClient();
		this.client.DefaultRequestHeaders.Accept.TryParseAdd("application/vnd.github+json");
		this.client.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");
		this.client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
	}

	public async Task CheckForUpdates() {
		await Task.Run(UpdateManager.UpdateCleanup);
		await this.RunUpdateCheck();
	}

	private async Task RunUpdateCheck() {
		Release? release = null;

		try {
			HttpResponseMessage res = await this.client.GetAsync(UpdateManager.API_URL + $"/releases/latest");
			res.EnsureSuccessStatusCode();
			release = Release.FromJson(await res.Content.ReadAsStringAsync());
		} catch (Exception) {
			return;
		}

		if (release == null) {
			return;
		}

		string currentVersion = "v" + Application.ProductVersion;
		if (currentVersion.Equals(release.TagName)) {
			return;
		}

		this.latestTag = release.TagName;
		this.mainForm.Invoke(() => {
			UpdateForm updateForm = new(this, release);
			updateForm.ShowDialog(this.mainForm);
		});
	}

	public async Task PerformUpdate(UpdateForm updateForm, Release release) {
		if (this.latestTag == null) {
			return;
		}

		ReleaseAsset? newVersion = null;
		ReleaseAsset[] assets = release.Assets;
		foreach (ReleaseAsset asset in assets) {
			if (asset.Name.Equals(UpdateManager.ASSET_NAME)) {
				newVersion = asset;
				break;
			}
		}

		if (newVersion == null) {
			updateForm.Invoke(() => {
				MessageBox.Show(
					updateForm,
					"There was an error while trying to lookup the update details.\n" +
						"Please try again later.", "Download Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error
				);
			});

			return;
		}

		string outputDir = Directory.GetCurrentDirectory();
		string exePath = Environment.ProcessPath!;
		string dllPath = Path.Join(outputDir, SA2Manager.HELPER_DLL_NAME);
		string downloadPath = Path.Join(outputDir, UpdateManager.ASSET_NAME);

		try {
			UriBuilder downloadUrlBuilder = new(newVersion.BrowserDownloadUrl) {
				Scheme = Uri.UriSchemeHttps,
				Port = -1
			};

			HttpResponseMessage download = await this.client.GetAsync(downloadUrlBuilder.Uri);
			download.EnsureSuccessStatusCode();

			using (FileStream fileStream = new(downloadPath, FileMode.CreateNew)) {
				await download.Content.CopyToAsync(fileStream);
			}

			if (!UpdateManager.VerifyFileHash(downloadPath, newVersion.Digest)) {
				throw new InvalidDataException("Downloaded update payload hash did not match release digest.");
			}
		} catch (Exception) {
			if (File.Exists(downloadPath)) {
				File.Delete(downloadPath);
			}

			updateForm.Invoke(() => {
				MessageBox.Show(
					updateForm,
					"There was an error while trying to download the update.\n" +
						"Please try again later.", "Download Error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error
				);
			});

			return;
		}

		try {
			File.Move(dllPath, dllPath + ".bak", true);
			File.Move(exePath, exePath + ".bak", true);

			ExtractionOptions options = new() {
				ExtractFullPath = true,
				Overwrite = true
			};

			using Stream stream = File.OpenRead(downloadPath);
			using IArchive archive = UpdateManager.OpenUpdateArchive(stream);
			foreach (IArchiveEntry entry in archive.Entries.Where((entry) => !entry.IsDirectory)) {
				await entry.WriteToDirectoryAsync(outputDir, options);
			}
		} catch (Exception) {
			if (File.Exists(dllPath + ".bak")) {
				if (File.Exists(dllPath)) {
					File.Delete(dllPath);
				}

				File.Move(dllPath + ".bak", dllPath, true);
			}

			if (File.Exists(exePath + ".bak")) {
				if (File.Exists(exePath)) {
					File.Delete(exePath);
				}

				File.Move(exePath + ".bak", exePath, true);
			}

			updateForm.Invoke(() => {
				MessageBox.Show(
					updateForm,
					"There was an error while trying to read the update contents.\n" +
						"Please try again later.", "Download Error ",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error
				);
			});

			return;
		} finally {
			if (File.Exists(downloadPath)) {
				File.Delete(downloadPath);
			}
		}

		updateForm.Invoke(updateForm.Close);
		Process.Start(exePath);
		Application.Exit();
	}

	private static IArchive OpenUpdateArchive(Stream stream) {
		SevenZipFactory factory = new();
		if (!factory.IsArchive(stream, ReaderOptions.ForExternalStream)) {
			throw new InvalidDataException("Update archive is not a SevenZip archive.");
		}

		if (stream.CanSeek) {
			stream.Position = 0;
		}

		return factory.OpenArchive(stream);
	}

	private static bool VerifyFileHash(string filePath, string hash) {
		try {
			if (string.IsNullOrWhiteSpace(hash)) {
				return false;
			}

			string[] hashParts = hash.Split(':', 2, StringSplitOptions.TrimEntries);
			if (hashParts.Length != 2 || string.IsNullOrWhiteSpace(hashParts[0]) || string.IsNullOrWhiteSpace(hashParts[1])) {
				return false;
			}

			using HashAlgorithm? algorithm = hashParts[0].ToLowerInvariant() switch {
				"sha256" => SHA256.Create(),
				"sha384" => SHA384.Create(),
				"sha512" => SHA512.Create(),
				_ => null
			};

			if (algorithm == null) {
				return false;
			}

			byte[] expectedHash = Convert.FromHexString(hashParts[1]);
			using FileStream stream = File.OpenRead(filePath);
			byte[] actualHash = algorithm.ComputeHash(stream);

			return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
		} catch (Exception) {}

		return false;
	}

	private static void UpdateCleanup() {
		int tries = 5;
		string dir = Directory.GetCurrentDirectory();
		string dllPath = Path.Join(dir, SA2Manager.HELPER_DLL_NAME + ".bak");

		while (File.Exists(dllPath) && tries-- > 0) {
			try {
				File.Delete(dllPath);
			} catch {
				Thread.Sleep(1000);
			}
		}

		if (Environment.ProcessPath == null) {
			return;
		}

		tries = 5;
		string exePath = Environment.ProcessPath + ".bak";
		while (File.Exists(exePath) && tries-- > 0) {
			try {
				File.Delete(exePath);
			} catch {
				Thread.Sleep(1000);
			}
		}
	}
}
