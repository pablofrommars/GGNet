using GGNet.Mcp;

namespace GGNet.Evals;

// The standalone validator script is the dependency-free twin of the MCP
// validate_plot tool (deliberate duplication — the script must run with
// nothing but the dotnet CLI). This smoke test keeps the script itself
// CI-exercised so the twins cannot drift silently.
public class ValidatorScriptEvals
{
	[Fact]
	public async Task ScriptValidatesAMinimalSnippet()
	{
		// Arrange

		var root = RepoRoot();
		var snippet = Path.Combine(Path.GetTempPath(), $"ggnet-script-smoke-{Guid.NewGuid():N}.cs");

		await File.WriteAllTextAsync(snippet,
			"""
			var xy = new[] { (X: 1.0, Y: 2.0), (X: 2.0, Y: 3.5) };
			var plot = PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style();
			Console.WriteLine((await plot.AsStringAsync()).Length);
			""", TestContext.Current.CancellationToken);

		try
		{
			// Act

			var result = await ProcessCapture.RunAsync(new ProcessStartInfo("dotnet",
				["run", Path.Combine(root, "skills", "ggnet", "scripts", "validate.cs"), "--", snippet])
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			}, TestContext.Current.CancellationToken);

			// Assert

			result.ExitStatus.ExitCode.Should().Be(0, $"stdout: {result.StandardOutput}; stderr: {result.StandardError}");
		}
		finally
		{
			File.Delete(snippet);
		}
	}

	private static string RepoRoot([CallerFilePath] string path = "")
		=> Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));
}
