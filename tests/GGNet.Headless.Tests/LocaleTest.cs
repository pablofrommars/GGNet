using System.Globalization;
using System.Text.RegularExpressions;

namespace GGNet.Headless.Tests;

// SVG geometry must serialize with '.' decimals and '-' minus regardless of the
// host culture; no global culture workaround is allowed in the library.
public class LocaleTest
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

	[Theory]
	[InlineData("sv-SE")]
	[InlineData("de-DE")]
	[InlineData("fr-FR")]
	public async Task GeometryIsCultureInvariant(string culture)
	{
		string invariant;
		using (new CultureScope(""))
		{
			invariant = Normalize(await RenderAsync());
		}

		string localized;
		using (new CultureScope(culture))
		{
			localized = Normalize(await RenderAsync());
		}

		Assert.Equal(invariant, localized);

		Assert.DoesNotMatch(@"\d,\d", localized);
		Assert.DoesNotContain('−', localized);

		System.Xml.Linq.XDocument.Parse(localized);
	}

	[Fact]
	public async Task PolarGeometryIsCultureInvariant()
	{
		using var _ = new CultureScope("sv-SE");

		var plot = PlotContext.Build(xy, i => i.X, i => i.Y)
			.Geom_Point()
			.Coord_Polar()
			.Style();

		var svg = await plot.AsStringAsync();

		Assert.DoesNotMatch(@"\d,\d", svg);
	}
}
