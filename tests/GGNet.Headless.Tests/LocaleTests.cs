namespace GGNet.Headless.Tests;

public class LocaleTests
{
	private sealed class CultureScope : IDisposable
	{
		private readonly CultureInfo previousCulture = CultureInfo.CurrentCulture;
		private readonly CultureInfo previousUICulture = CultureInfo.CurrentUICulture;

		public CultureScope(string name)
		{
			var culture = CultureInfo.GetCultureInfo(name);

			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = culture;
		}

		public void Dispose()
		{
			CultureInfo.CurrentCulture = previousCulture;
			CultureInfo.CurrentUICulture = previousUICulture;
		}
	}

	private sealed record XY(double X, double Y);

	private static readonly XY[] xy =
	[
		new(1.5, -2.25),
		new(2.5, 3.75),
		new(3.5, 2.125)
	];

	private static string Normalize(string svg) => Regex.Replace(svg, "gg[A-Za-z0-9_-]+", "ggID");

	private static async Task<string> RenderAsync()
	{
		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Point()
			.Geom_Line(xy, i => i.X, i => i.Y)
			.Style();

		return await plot.AsStringAsync();
	}

	[Fact]
	public async Task GeometryIsCultureInvariantAcrossAllCultures()
	{
		// Arrange

		string invariant;
		using (new CultureScope(""))
		{
			invariant = Normalize(await RenderAsync());
		}

		// Act / Assert

		// Every installed culture, not three hand-picked ones: exotic cultures
		// (non-Latin digits, U+2212 minus) are where invariance bugs hide.
		foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
		{
			if (string.IsNullOrEmpty(culture.Name))
			{
				continue;
			}

			string localized;
			using (new CultureScope(culture.Name))
			{
				localized = Normalize(await RenderAsync());
			}

			using var _ = new AssertionScope();

			localized.Should().Be(invariant, "culture '{0}' must render invariant geometry", culture.Name);
			localized.Should().NotMatchRegex(@"\d,\d", "culture '{0}'", culture.Name);
			localized.Should().NotContain("−", "culture '{0}'", culture.Name);

			XDocument.Parse(localized);
		}
	}

	[Fact]
	public async Task LocalizedTicksAreAnExplicitOptIn()
	{
		// Arrange

		var culture = CultureInfo.GetCultureInfo("sv-SE");

		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Scale_Y_Continuous(formatter: new Formats.DoubleFormatter("N1", culture))
			.Geom_Point()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		// Label text localizes (comma decimals) ...
		svg.Should().MatchRegex(@">-?\d+,\d</text>");

		// ... while geometry stays invariant: no decimal comma inside any
		// attribute value (translate(x, y) separators carry a space).
		foreach (var attribute in XDocument.Parse(svg).Descendants().Attributes())
		{
			attribute.Value.Should().NotMatchRegex(@"\d,\d", $"attribute '{attribute.Name}' must serialize invariantly");
		}
	}

	[Fact]
	public async Task PolarGeometryIsCultureInvariant()
	{
		// Arrange

		using var _ = new CultureScope("sv-SE");

		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Point()
			.Coord_Polar()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		svg.Should().NotMatchRegex(@"\d,\d");
	}

	[Fact]
	public async Task PolarBreakTitlesAreCultureInvariant()
	{
		// Arrange

		using var culture = new CultureScope("sv-SE");

		// Break titles add a second grid-label pass and an extra gutter term
		// under polar; both the title text (default-invariant formatter) and
		// the stacked-label geometry must ignore the ambient culture.
		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Scale_X_Discrete(expand: (0.0, 0.0, 0.0, 1.0), titles: new Formats.DoubleFormatter("N1"))
			.Geom_Point()
			.Coord_Polar()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		svg.Should().Contain("x-break-title");
		svg.Should().NotMatchRegex(@"\d,\d");
		svg.Should().NotContain("−");
	}
}
