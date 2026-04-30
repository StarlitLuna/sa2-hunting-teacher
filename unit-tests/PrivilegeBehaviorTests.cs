using sa2_hunting_teacher;
using System.Reflection;
using System.Xml.Linq;

namespace unit_tests;

public class PrivilegeBehaviorTests {
	[Fact]
	public void AppManifest_UsesAsInvokerExecutionLevel() {
		XDocument manifest = XDocument.Load(ProjectFile("sa2-hunting-teacher", "app.manifest"));
		XNamespace securityNs = "urn:schemas-microsoft-com:asm.v3";

		XElement requestedExecutionLevel = Assert.Single(manifest.Descendants(securityNs + "requestedExecutionLevel"));

		Assert.Equal("asInvoker", requestedExecutionLevel.Attribute("level")?.Value);
	}

	[Fact]
	public void ProgramMain_DoesNotBlockUnelevatedProcesses() {
		string source = File.ReadAllText(ProjectFile("sa2-hunting-teacher", "Program.cs"));

		Assert.DoesNotContain("Environment.IsPrivilegedProcess", source);
		Assert.DoesNotContain("Privileges Required", source);
	}

	[Fact]
	public void SA2Manager_UsesTargetedAccessDeniedInjectionMessage() {
		MethodInfo? method = typeof(SA2Manager).GetMethod(
			"GetInjectionFailureMessage",
			BindingFlags.Static | BindingFlags.NonPublic
		);
		Assert.NotNull(method);

		string message = Assert.IsType<string>(method!.Invoke(null, [5]));

		Assert.Contains("SA2 appears to be running elevated", message);
		Assert.Contains("restart SA2 normally", message);
		Assert.Contains("run this tool as administrator", message);
	}

	private static string ProjectFile(params string[] parts) {
		DirectoryInfo? current = new(AppContext.BaseDirectory);
		while (current != null && !File.Exists(Path.Join(current.FullName, "sa2-hunting-teacher.sln"))) {
			current = current.Parent;
		}

		Assert.NotNull(current);
		return Path.Join([current!.FullName, .. parts]);
	}
}
