namespace GGNet.Headless.Tests;

// Geospacial.Polygon.Hole was public API with no reader: every ring became an ordinary subpath
// and whether a declared hole rendered depended on the winding the caller happened to supply.
// Composition now normalizes orientation per ring — exteriors one way, holes the other — which
// is what SVG's default nonzero rule needs.
public class PolygonHoleTests
{
	private sealed record Region(Geospacial.Polygon[] Shapes);

	private static Geospacial.Polygon Ring(double[] longitude, double[] latitude, bool hole = false)
		=> new() { Longitude = longitude, Latitude = latitude, Hole = hole };

	private static Geospacial.Polygon Reversed(Geospacial.Polygon poly)
		=> new()
		{
			Longitude = [.. poly.Longitude.Reverse()],
			Latitude = [.. poly.Latitude.Reverse()],
			Hole = poly.Hole
		};

	// A square with a smaller square punched out of its middle.
	private static readonly Geospacial.Polygon exterior = Ring([0.0, 10.0, 10.0, 0.0], [0.0, 0.0, 10.0, 10.0]);
	private static readonly Geospacial.Polygon hole = Ring([3.0, 7.0, 7.0, 3.0], [3.0, 3.0, 7.0, 7.0], hole: true);

	private static async Task<string> Path(Geospacial.Polygon[] polygons)
	{
		var svg = await PlotContext.Build([new Region(polygons)])
			.Geom_Map(r => r.Shapes)
			.Style()
			.AsStringAsync();

		return XDocument.Parse(svg).Descendants()
			.First(element => element.Name.LocalName == "path")
			.Attribute("d")!.Value;
	}

	private static (double x, double y)[] Ring(string d, int index)
		=> [.. d.Split('M', StringSplitOptions.RemoveEmptyEntries)[index]
			.Replace("Z", string.Empty, StringComparison.Ordinal)
			.Split('L', StringSplitOptions.RemoveEmptyEntries)
			.Select(point => point.Trim().Split(' '))
			.Select(parts => (
				double.Parse(parts[0], CultureInfo.InvariantCulture),
				double.Parse(parts[1], CultureInfo.InvariantCulture)))];

	private static double SignedArea((double x, double y)[] ring)
	{
		var area = 0.0;

		for (var i = 0; i < ring.Length; i++)
		{
			var (x1, y1) = ring[i];
			var (x2, y2) = ring[(i + 1) % ring.Length];

			area += (x1 * y2) - (x2 * y1);
		}

		return area;
	}

	[Fact]
	public async Task HoleIsWoundAgainstItsExterior()
	{
		// Arrange / Act

		var d = await Path([exterior, hole]);

		// Assert

		using var _ = new AssertionScope();

		SignedArea(Ring(d, 0)).Should().BePositive();
		SignedArea(Ring(d, 1)).Should().BeNegative();
	}

	[Fact]
	public async Task SourceWindingDoesNotMatter()
	{
		// Arrange

		// Every combination of caller-supplied winding must compose to the same path.
		var expected = await Path([exterior, hole]);

		// Act

		var reversedExterior = await Path([Reversed(exterior), hole]);
		var reversedHole = await Path([exterior, Reversed(hole)]);
		var bothReversed = await Path([Reversed(exterior), Reversed(hole)]);

		// Assert

		using var _ = new AssertionScope();

		reversedExterior.Should().Be(expected);
		reversedHole.Should().Be(expected);
		bothReversed.Should().Be(expected);
	}

	[Fact]
	public async Task OverlappingExteriorsStayFilled()
	{
		// Arrange

		var a = Ring([0.0, 6.0, 6.0, 0.0], [0.0, 0.0, 6.0, 6.0]);
		var b = Ring([4.0, 10.0, 10.0, 4.0], [4.0, 4.0, 10.0, 10.0]);

		// Act

		var d = await Path([a, Reversed(b)]);

		// Assert

		// Same winding on both, so their overlap accumulates instead of cancelling — the
		// failure mode a blanket fill-rule="evenodd" would have introduced.
		using var _ = new AssertionScope();

		var first = SignedArea(Ring(d, 0));
		var second = SignedArea(Ring(d, 1));

		first.Should().BePositive();
		second.Should().BePositive();
	}

	[Fact]
	public async Task NoFillRuleIsEmitted()
	{
		// Arrange

		var svg = await PlotContext.Build([new Region([exterior, hole])])
			.Geom_Map(r => r.Shapes)
			.Style()
			.AsStringAsync();

		// Act / Assert

		// Holes are carried by winding under the default nonzero rule, not by a fill rule.
		svg.Should().NotContain("fill-rule");
	}
}
