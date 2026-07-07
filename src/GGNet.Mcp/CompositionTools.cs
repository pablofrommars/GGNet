namespace GGNet.Mcp;

using System.Diagnostics;
using System.Reflection;

[McpServerToolType]
public static class CompositionTools
{
	[McpServerTool(Name = "list_geoms"), Description(
		"Live GGNet geom surface: every Geom_* entry point with its distinct parameter shapes — reflected from the loaded assembly, never stale.")]
	public static string ListGeoms()
	{
		return Introspect("Geom_");
	}

	[McpServerTool(Name = "list_scales"), Description(
		"Live GGNet scale/facet/composition surface: Scale_*, Facet_*, Flip, Coord_Polar and label entry points with their distinct parameter shapes — reflected from the loaded assembly, never stale.")]
	public static string ListScales()
	{
		return Introspect("Scale_", "Facet_", "Flip", "Coord_Polar", "Title", "SubTitle", "Caption", "XLab", "YLab", "XLim", "YLim", "Panel", "Style");
	}

	[McpServerTool(Name = "validate_plot"), Description(
		"Compile (warnings-as-errors) and render a GGNet snippet to SVG headlessly. The snippet is a C# top-level-statements program that builds a plot and renders it, e.g. ends with Console.WriteLine((await plot.AsStringAsync()).Length). Global usings provided: GGNet, GGNet.Formats, GGNet.Headless, NodaTime. Returns ok + output, or the compiler/render errors to fix.")]
	public static async Task<string> ValidatePlot(
		[Description("C# top-level-statements snippet that builds and renders a plot.")] string snippet,
		CancellationToken cancellationToken = default)
	{
		var root = RepoRoot();
		var scratch = Directory.CreateTempSubdirectory("ggnet-mcp-validate-");

		try
		{
			await File.WriteAllTextAsync(Path.Combine(scratch.FullName, "Snippet.csproj"),
				$"""
				<Project Sdk="Microsoft.NET.Sdk">

				  <PropertyGroup>
				    <OutputType>Exe</OutputType>
				    <TargetFramework>net11.0</TargetFramework>
				    <ImplicitUsings>enable</ImplicitUsings>
				    <Nullable>enable</Nullable>
				    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
				  </PropertyGroup>

				  <ItemGroup>
				    <ProjectReference Include="{root}/src/GGNet/GGNet.csproj" />
				    <ProjectReference Include="{root}/src/GGNet.Headless/GGNet.Headless.csproj" />
				  </ItemGroup>

				</Project>
				""", cancellationToken);

			await File.WriteAllTextAsync(Path.Combine(scratch.FullName, "GlobalUsings.cs"),
				"""
				global using GGNet;
				global using GGNet.Formats;
				global using GGNet.Headless;
				global using NodaTime;
				""", cancellationToken);

			await File.WriteAllTextAsync(Path.Combine(scratch.FullName, "Program.cs"), snippet, cancellationToken);

			var run = new Process
			{
				StartInfo = new ProcessStartInfo("dotnet", ["run", "--project", scratch.FullName, "-v", "q"])
				{
					RedirectStandardOutput = true,
					RedirectStandardError = true,
				}
			};

			run.Start();

			var stdout = await run.StandardOutput.ReadToEndAsync(cancellationToken);
			var stderr = await run.StandardError.ReadToEndAsync(cancellationToken);
			await run.WaitForExitAsync(cancellationToken);

			return new JsonObject
			{
				["ok"] = run.ExitCode == 0,
				["output"] = stdout.Trim(),
				["errors"] = run.ExitCode == 0 ? null : (stdout + stderr).Trim(),
			}.ToJsonString();
		}
		finally
		{
			scratch.Delete(recursive: true);
		}
	}

	private static string Introspect(params string[] prefixes)
	{
		var families = typeof(BuilderExtensions)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Concat(typeof(Stat).GetMethods(BindingFlags.Public | BindingFlags.Static))
			.Where(m => prefixes.Any(p => m.Name == p.TrimEnd('_') || m.Name.StartsWith(p, StringComparison.Ordinal)))
			.GroupBy(m => m.Name)
			.OrderBy(g => g.Key, StringComparer.Ordinal);

		var result = new JsonObject();

		foreach (var family in families)
		{
			var shapes = family
				.Select(m => string.Join(", ", m.GetParameters().Skip(1).Select(p => p.Name)))
				.Distinct()
				.Select(s => (JsonNode)s);

			result[family.Key] = new JsonArray([.. shapes]);
		}

		return result.ToJsonString();
	}

	private static string RepoRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);

		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GGNet.slnx")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName
			?? throw new InvalidOperationException("GGNet.slnx not found above the server binary; validate_plot needs the repo checkout.");
	}
}
