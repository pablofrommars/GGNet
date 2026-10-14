namespace GGNet.Headless;

// INVARIANT: theme files use .ggnet only as a selector prefix — self-contained export re-roots them
// onto svg by plain string replacement, so any other use corrupts the embedded stylesheet.
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
