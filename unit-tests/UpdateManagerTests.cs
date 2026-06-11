using sa2_hunting_teacher;
using sa2_hunting_teacher.Updates;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;

namespace unit_tests;

public class UpdateManagerTests {
	[Fact]
	public void Constructor_ConfiguresGitHubHttpHeaders() {
		UpdateManager manager = BuildManager();

		HttpClient client = GetClient(manager);

		Assert.Contains(client.DefaultRequestHeaders.Accept, header => header.MediaType == "application/vnd.github+json");
		Assert.Contains(client.DefaultRequestHeaders.UserAgent, header => header.Product?.Name == "Mozilla");
		Assert.True(client.DefaultRequestHeaders.TryGetValues("X-GitHub-Api-Version", out IEnumerable<string>? values));
		Assert.Contains("2022-11-28", values);
	}

	[Fact]
	public async Task RunUpdateCheck_ReturnsWithoutLatestTag_WhenLatestReleaseRequestFails() {
		StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
		UpdateManager manager = BuildManager(handler);

		await InvokeAsync(manager, "RunUpdateCheck");

		Assert.Null(GetLatestTag(manager));
		Assert.Single(handler.Requests);
		Assert.Equal("https://api.github.com/repos/StarlitLuna/sa2-hunting-teacher/releases/latest", handler.Requests[0].RequestUri?.ToString());
	}

	[Fact]
	public async Task RunUpdateCheck_ReturnsWithoutLatestTag_WhenLatestReleaseJsonIsInvalid() {
		StubHttpMessageHandler handler = new(_ => JsonResponse("{ this is not json"));
		UpdateManager manager = BuildManager(handler);

		await InvokeAsync(manager, "RunUpdateCheck");

		Assert.Null(GetLatestTag(manager));
		Assert.Single(handler.Requests);
	}

	[Fact]
	public async Task RunUpdateCheck_ReturnsWithoutLatestTag_WhenLatestReleaseJsonIsNull() {
		StubHttpMessageHandler handler = new(_ => JsonResponse("null"));
		UpdateManager manager = BuildManager(handler);

		await InvokeAsync(manager, "RunUpdateCheck");

		Assert.Null(GetLatestTag(manager));
		Assert.Single(handler.Requests);
	}

	[Fact]
	public async Task RunUpdateCheck_ReturnsWithoutLatestTag_WhenReleaseMatchesCurrentVersion() {
		string currentTag = "v" + Application.ProductVersion;
		StubHttpMessageHandler handler = new(_ => JsonResponse(ReleaseJson(currentTag)));
		UpdateManager manager = BuildManager(handler);

		await InvokeAsync(manager, "RunUpdateCheck");

		Assert.Null(GetLatestTag(manager));
		Assert.Single(handler.Requests);
	}

	[Fact]
	public async Task CheckForUpdates_RunsCleanupBeforeNoOpUpdateCheck() {
		StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
		UpdateManager manager = BuildManager(handler);

		await InTemporaryCurrentDirectory(async temp => {
			string helperBackup = Path.Combine(temp, SA2Manager.HELPER_DLL_NAME + ".bak");
			File.WriteAllText(helperBackup, "stale helper backup");

			await manager.CheckForUpdates();

			Assert.False(File.Exists(helperBackup));
		});

		Assert.Single(handler.Requests);
		Assert.Null(GetLatestTag(manager));
	}

	[Fact]
	public async Task PerformUpdate_ReturnsWithoutUsingForm_WhenLatestTagIsUnset() {
		UpdateManager manager = BuildManager();
		Release release = new() { Assets = [] };

		Exception? thrown = await Record.ExceptionAsync(() => manager.PerformUpdate(null!, release));

		Assert.Null(thrown);
	}

	[Fact]
	public async Task PerformUpdate_ForcesPayloadDownloadUrlToHttpsDefaultPort() {
		StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
		UpdateManager manager = BuildManager(handler);
		SetLatestTag(manager, "v2.0.0");
		Release release = new() {
			Assets = [
				new ReleaseAsset {
					Name = "sa2-hunting-teacher.7z",
					BrowserDownloadUrl = new Uri("http://downloads.example.test:8080/update.7z?next=http://downloads.example.test/fallback"),
					Digest = "sha256:4ec21996023342216e26288875756414f96c7ec997cf7e51d46bd636bb5790e6"
				}
			]
		};

		await InTemporaryCurrentDirectory(async _ => {
			await Record.ExceptionAsync(() => manager.PerformUpdate(null!, release));
		});

		HttpRequestMessage request = Assert.Single(handler.Requests);
		Assert.NotNull(request.RequestUri);
		Uri requestUri = request.RequestUri;
		Assert.Equal(Uri.UriSchemeHttps, requestUri.Scheme);
		Assert.True(requestUri.IsDefaultPort);
		Assert.Equal("downloads.example.test", requestUri.Host);
		Assert.Equal("/update.7z", requestUri.AbsolutePath);
		Assert.Equal("?next=http://downloads.example.test/fallback", requestUri.Query);
	}

	[Fact]
	public void VerifyFileHash_ReturnsTrue_WhenSha256DigestMatches() {
		InTemporaryCurrentDirectory(temp => {
			string file = Path.Combine(temp, "update.7z");
			File.WriteAllText(file, "upgrade payload");

			bool matches = VerifyFileHash(file, "sha256:4ec21996023342216e26288875756414f96c7ec997cf7e51d46bd636bb5790e6");

			Assert.True(matches);
		});
	}

	[Fact]
	public void VerifyFileHash_ReturnsFalse_WhenSha256DigestMismatches() {
		InTemporaryCurrentDirectory(temp => {
			string file = Path.Combine(temp, "update.7z");
			File.WriteAllText(file, "tampered payload");

			bool matches = VerifyFileHash(file, "sha256:4ec21996023342216e26288875756414f96c7ec997cf7e51d46bd636bb5790e6");

			Assert.False(matches);
		});
	}

	[Theory]
	[InlineData("")]
	[InlineData("sha256")]
	[InlineData("sha256:not-hex")]
	[InlineData("sha999:4ec21996023342216e26288875756414f96c7ec997cf7e51d46bd636bb5790e6")]
	public void VerifyFileHash_ReturnsFalse_WhenDigestCannotBeChecked(string digest) {
		InTemporaryCurrentDirectory(temp => {
			string file = Path.Combine(temp, "update.7z");
			File.WriteAllText(file, "upgrade payload");

			bool matches = VerifyFileHash(file, digest);

			Assert.False(matches);
		});
	}

	[Fact]
	public void OpenUpdateArchive_OpensSevenZipArchives() {
		using MemoryStream archiveStream = BuildSevenZipArchive();

		using IArchive archive = Assert.IsAssignableFrom<IArchive>(OpenUpdateArchive(archiveStream));

		IArchiveEntry entry = Assert.Single(archive.Entries, entry => !entry.IsDirectory);
		Assert.Equal("update.txt", entry.Key);
	}

	[Fact]
	public void OpenUpdateArchive_RejectsNonSevenZipArchives() {
		using MemoryStream archive = BuildZipArchive();

		TargetInvocationException ex = Assert.Throws<TargetInvocationException>(
			() => OpenUpdateArchive(archive)
		);

		InvalidDataException inner = Assert.IsType<InvalidDataException>(ex.InnerException);
		Assert.Equal("Update archive is not a SevenZip archive.", inner.Message);
	}

	[Fact]
	public void UpdateCleanup_DeletesHelperBackupFromCurrentDirectory() {
		InTemporaryCurrentDirectory(temp => {
			string helperBackup = Path.Combine(temp, SA2Manager.HELPER_DLL_NAME + ".bak");
			File.WriteAllText(helperBackup, "stale helper backup");

			InvokeStatic("UpdateCleanup");

			Assert.False(File.Exists(helperBackup));
		});
	}

	[Fact]
	public void UpdateCleanup_DeletesExecutableBackupWhenProcessPathExists() {
		Assert.False(string.IsNullOrEmpty(Environment.ProcessPath));
		string executableBackup = Environment.ProcessPath + ".bak";
		if (File.Exists(executableBackup)) {
			File.Delete(executableBackup);
		}

		try {
			File.WriteAllText(executableBackup, "stale executable backup");

			InvokeStatic("UpdateCleanup");

			Assert.False(File.Exists(executableBackup));
		} finally {
			if (File.Exists(executableBackup)) {
				File.Delete(executableBackup);
			}
		}
	}

	private static MemoryStream BuildSevenZipArchive() {
		MemoryStream stream = new();
		WriterOptions options = new(CompressionType.LZMA2) { LeaveStreamOpen = true };
		using (IWriter writer = WriterFactory.OpenWriter(stream, ArchiveType.SevenZip, options)) {
			using MemoryStream file = new(Encoding.UTF8.GetBytes("sevenzip update payload"));
			writer.Write("update.txt", file, DateTime.UtcNow);
		}

		stream.Position = 0;
		return stream;
	}

	private static MemoryStream BuildZipArchive() {
		MemoryStream stream = new();
		using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true)) {
			ZipArchiveEntry entry = archive.CreateEntry("update.txt");
			using StreamWriter writer = new(entry.Open());
			writer.Write("not a sevenzip update");
		}

		stream.Position = 0;
		return stream;
	}

	private sealed class StubHttpMessageHandler : HttpMessageHandler {
		private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> responses = new();

		public List<HttpRequestMessage> Requests { get; } = [];

		public StubHttpMessageHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responses) {
			foreach (Func<HttpRequestMessage, HttpResponseMessage> response in responses) {
				this.responses.Enqueue(response);
			}
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
			this.Requests.Add(request);
			if (this.responses.Count == 0) {
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
			}

			return Task.FromResult(this.responses.Dequeue()(request));
		}
	}

	private static UpdateManager BuildManager(HttpMessageHandler? handler = null) {
		HuntingTeacherForm form = (HuntingTeacherForm)RuntimeHelpers.GetUninitializedObject(typeof(HuntingTeacherForm));
		UpdateManager manager = new(form);
		if (handler != null) {
			Reflect.SetField(manager, "client", new HttpClient(handler));
		}

		return manager;
	}

	private static Task InvokeAsync(object target, string methodName) {
		return (Task)Reflect.Invoke(target, methodName)!;
	}

	private static void SetLatestTag(UpdateManager manager, string? tag) {
		Reflect.SetField(manager, "latestTag", tag);
	}

	private static string? GetLatestTag(UpdateManager manager) {
		return Reflect.GetField<string?>(manager, "latestTag");
	}

	private static HttpClient GetClient(UpdateManager manager) {
		return Reflect.GetField<HttpClient>(manager, "client");
	}

	private static HttpResponseMessage JsonResponse(string json) {
		return new HttpResponseMessage(HttpStatusCode.OK) {
			Content = new StringContent(json, Encoding.UTF8, "application/json")
		};
	}

	private static string ReleaseJson(string tagName) {
		return $$"""
			{
				"tag_name": "{{tagName}}",
				"assets": []
			}
			""";
	}

	private static void InTemporaryCurrentDirectory(Action<string> action) {
		string original = Directory.GetCurrentDirectory();
		string temp = Path.Combine(Path.GetTempPath(), "sa2-update-tests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(temp);

		try {
			Directory.SetCurrentDirectory(temp);
			action(temp);
		} finally {
			Directory.SetCurrentDirectory(original);
			if (Directory.Exists(temp)) {
				Directory.Delete(temp, recursive: true);
			}
		}
	}

	private static async Task InTemporaryCurrentDirectory(Func<string, Task> action) {
		string original = Directory.GetCurrentDirectory();
		string temp = Path.Combine(Path.GetTempPath(), "sa2-update-tests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(temp);

		try {
			Directory.SetCurrentDirectory(temp);
			await action(temp);
		} finally {
			Directory.SetCurrentDirectory(original);
			if (Directory.Exists(temp)) {
				Directory.Delete(temp, recursive: true);
			}
		}
	}

	private static object? OpenUpdateArchive(Stream stream) {
		MethodInfo method = typeof(UpdateManager).GetMethod("OpenUpdateArchive", BindingFlags.Static | BindingFlags.NonPublic)!;
		Assert.NotNull(method);
		return method.Invoke(null, [stream]);
	}

	private static bool VerifyFileHash(string path, string digest) {
		MethodInfo method = typeof(UpdateManager).GetMethod("VerifyFileHash", BindingFlags.Static | BindingFlags.NonPublic)!;
		Assert.NotNull(method);
		return (bool)method.Invoke(null, [path, digest])!;
	}

	private static void InvokeStatic(string methodName) {
		MethodInfo method = typeof(UpdateManager).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;
		Assert.NotNull(method);
		method.Invoke(null, []);
	}
}
