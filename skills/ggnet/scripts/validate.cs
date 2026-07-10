// Validate a generated GGNet snippet: compile under warnings-as-errors against
// the in-repo GGNet + GGNet.Headless, then execute it (the snippet renders SVG).
//
// Usage: dotnet run skills/ggnet/scripts/validate.cs -- <snippet.cs>
//
// Contract: the snippet is a top-level-statements C# file that builds a plot
// and renders it, e.g.
//
//   var xy = new[] { (X: 1.0, Y: 2.0), (X: 2.0, Y: 3.5) };
//   var plot = PlotContext.Build(xy, i => i.X, i => i.Y).Geom_Point().Style();
//   Console.WriteLine((await plot.AsStringAsync()).Length);
//
// Global usings provided: GGNet, GGNet.Formats, GGNet.Headless, NodaTime.
// Exit 0 = compiles and renders; non-zero = fix the snippet (see reference/
// and patterns/common-mistakes.md).

using System.Diagnostics;
using System.Runtime.CompilerServices;

if (args is not [var snippetPath] || !File.Exists(snippetPath))
{
	Console.Error.WriteLine("usage: dotnet run validate.cs -- <snippet.cs>");
	return 2;
}

var root = Path.GetFullPath(Path.Combine(ScriptDirectory(), "..", "..", ".."));
var scratch = Directory.CreateTempSubdirectory("ggnet-validate-");

try
{
	File.WriteAllText(Path.Combine(scratch.FullName, "Snippet.csproj"),
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
		""");

	File.WriteAllText(Path.Combine(scratch.FullName, "GlobalUsings.cs"),
		"""
		global using GGNet;
		global using GGNet.Formats;
		global using GGNet.Headless;
		global using NodaTime;
		""");

	File.Copy(snippetPath, Path.Combine(scratch.FullName, "Program.cs"));

	using var run = Process.Start(new ProcessStartInfo("dotnet", ["run", "--project", scratch.FullName, "-v", "q"]))!;
	run.WaitForExit();

	if (run.ExitCode == 0)
	{
		Console.WriteLine($"validate: OK ({Path.GetFileName(snippetPath)} compiles and renders)");
	}

	return run.ExitCode;
}
finally
{
	scratch.Delete(recursive: true);
}

static string ScriptDirectory([CallerFilePath] string path = "")
	=> Path.GetDirectoryName(path)!;
