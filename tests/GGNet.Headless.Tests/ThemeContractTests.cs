namespace GGNet.Headless.Tests;

// The theming contract, machine-checked: every class the markup can emit is
// painted by the base theme, every referenced --ggnet-* variable is defined,
// and theme override files set only variables the base declares — a typo'd
// or incomplete theme is a test failure, not a silently black plot.
public class ThemeContractTests
{
	// The paint vocabulary the components emit (structural classes like
	// cursor-pointer/animate-* are styled but not per-theme paint).
	private static readonly string[] painted =
	[
		"plot", "panel",
		"title", "sub-title", "caption",
		"x-strip", "y-strip",
		"x-break", "x-minor-break", "y-break", "y-minor-break",
		"x-break-label", "y-break-label", "x-break-title",
		"x-title", "y-title",
		"legend-title", "legend-label",
		"crosshair", "crosshair-label",
		"spinner"
	];

	private static string Load(string name)
	{
		using var stream = typeof(PlotContext).Assembly.GetManifestResourceStream($"GGNet.Themes.{name}.css")!;
		using var reader = new StreamReader(stream);

		return reader.ReadToEnd();
	}

	private static HashSet<string> Defined(string css)
		=> [.. Regex.Matches(css, @"(--ggnet-[a-z-]+)\s*:").Select(m => m.Groups[1].Value)];

	private static HashSet<string> Referenced(string css)
		=> [.. Regex.Matches(css, @"var\((--ggnet-[a-z-]+)").Select(m => m.Groups[1].Value)];

	[Fact]
	public void BaseThemePaintsEveryEmittedClass()
	{
		// Arrange

		var css = Load("Default");

		// Act

		var unpainted = painted.Where(c => !css.Contains($".ggnet .{c}", StringComparison.Ordinal)).ToList();

		// Assert

		unpainted.Should().BeEmpty();
	}

	[Fact]
	public void BaseThemeDefinesEveryVariableItReferences()
	{
		// Arrange

		var css = Load("Default");

		// Act

		var undefined = Referenced(css).Except(Defined(css)).ToList();

		// Assert

		undefined.Should().BeEmpty();
	}

	[Fact]
	public void ThemeOverridesOnlyKnownVariables()
	{
		// Arrange

		var known = Defined(Load("Default"));

		// Act

		var unknown = Defined(Load("DotnetInteractive")).Except(known).ToList();

		// Assert

		unknown.Should().BeEmpty();
	}

	[Fact]
	public async Task SelfContainedExportEmbedsTheThemeAndStaysWellFormed()
	{
		// Arrange

		var plot = PlotContext.Build([(x: 1.0, y: 2.0), (x: 2.0, y: 3.0)], i => i.x, i => i.y)
			.Geom_Point()
			.Style();

		// Act

		var svg = await plot.AsStringAsync(selfContained: true);

		// Assert

		using var _ = new AssertionScope();

		svg.Should().Contain("<style>").And.Contain("svg .panel");
		svg.Should().NotContain(".ggnet", "the wrapper div does not exist in pure-SVG export");

		XDocument.Parse(svg);
	}

	[Fact]
	public async Task SelfContainedExportRejectsUnbundledThemes()
	{
		// Arrange

		var plot = PlotContext.Build([(x: 1.0, y: 2.0)], i => i.x, i => i.y)
			.Geom_Point()
			.Style();

		// Act

		Func<Task> act = () => plot.AsStringAsync(theme: "zb", selfContained: true);

		// Assert

		await act.Should().ThrowAsync<GGNetUserException>().WithMessage("*bundled*");
	}
}
