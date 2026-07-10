using GGNet.Exceptions;

namespace GGNet.Headless;

// Produces the embeddable form of a bundled theme for self-contained export:
// the interactive selectors are scoped to the .ggnet wrapper div, which the
// pure-SVG export deliberately strips, so prefixes re-root onto the svg
// element. The theme files are authored for this transform to be a plain
// prefix replacement.
internal static class ThemeCss
{
	public static string SelfContained(string theme)
	{
		var css = theme switch
		{
			"default" => Load("Default"),
			"dotnet-interactive" => Load("Default") + "\n" + Load("DotnetInteractive").Replace(".ggnet[theme=dotnet-interactive]", ".ggnet"),
			_ => throw new GGNetUserException($"'{theme}' is not a bundled theme; self-contained export supports 'default' and 'dotnet-interactive'")
		};

		return css.Replace(".ggnet", "svg");
	}

	private static string Load(string name)
	{
		using var stream = typeof(PlotContext).Assembly.GetManifestResourceStream($"GGNet.Themes.{name}.css")
			?? throw new GGNetInternalException($"theme resource '{name}' missing");

		using var reader = new StreamReader(stream);

		return reader.ReadToEnd();
	}
}
