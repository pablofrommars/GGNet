namespace GGNet.Headless.Tests;

// The extractor's contract is byte-level: the gallery goldens are pinned to the
// exact spacing, self-closing set and escaping asserted here.
public class SvgExtractorTests
{
	private static string Extract(string html)
	{
		using var writer = new StringWriter();

		SvgExtractor.Write(html, writer);

		return writer.ToString();
	}

	[Fact]
	public void SelectsTheChartSvgOverTheSpinners()
	{
		// Arrange

		var html = """
			<div class="ggnet" b-scope><svg id="chart" b-scope><rect class="panel" b-scope></rect></svg><div class="spinner" b-scope><svg viewBox="0 0 100 101" b-scope><path class="track" d="M0 0" b-scope></path></svg></div></div>
			""";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be("<svg id=\"chart\">\n\t<rect class=\"panel\" />\n</svg>");
	}

	[Fact]
	public void SelectsTheChartSvgOverANestedOne()
	{
		// Arrange

		var html = """
			<div class="ggnet"><div class="decoy"><svg id="nested"></svg></div><svg id="chart"></svg></div>
			""";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be("<svg id=\"chart\"></svg>");
	}

	[Fact]
	public void ThrowsWhenTheComponentRendersNoChartSvg()
	{
		// Arrange

		var html = """
			<div class="ggnet"><div class="spinner"><svg viewBox="0 0 100 101"></svg></div></div>
			""";

		// Act

		Action act = () => Extract(html);

		// Assert

		act.Should().Throw<GGNetInternalException>();
	}

	[Fact]
	public void DropsScopedCssAttributes()
	{
		// Arrange

		var html = """
			<div class="ggnet" b-ikklo4v3l4><svg id="chart" b-ikklo4v3l4><rect class="plot" width="100%" b-ikklo4v3l4></rect></svg></div>
			""";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be("<svg id=\"chart\">\n\t<rect class=\"plot\" width=\"100%\" />\n</svg>");
	}

	[Fact]
	public void PreservesSvgCamelCaseNamesAndAttributes()
	{
		// Arrange

		var html = """
			<div><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 720 576"><clipPath id="a"><rect x="0"></rect></clipPath><linearGradient id="g" gradientUnits="userSpaceOnUse"><stop offset="0%"></stop></linearGradient></svg></div>
			""";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be(
			"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" viewBox=\"0 0 720 576\">"
			+ "\n\t<clipPath id=\"a\">\n\t\t<rect x=\"0\" />\n\t</clipPath>"
			+ "\n\t<linearGradient id=\"g\" gradientUnits=\"userSpaceOnUse\">\n\t\t<stop offset=\"0%\" />\n\t</linearGradient>"
			+ "\n</svg>");
	}

	[Theory]
	[InlineData("line")]
	[InlineData("circle")]
	[InlineData("rect")]
	[InlineData("path")]
	[InlineData("stop")]
	public void SelfClosesEmptyElementsInTheSet(string name)
	{
		// Arrange

		var html = $"""<div><svg><{name} class="x"></{name}></svg></div>""";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be($"<svg>\n\t<{name} class=\"x\" />\n</svg>");
	}

	[Theory]
	[InlineData("defs")]
	[InlineData("g")]
	[InlineData("text")]
	[InlineData("clipPath")]
	public void WritesAnOpenClosePairForEveryOtherEmptyElement(string name)
	{
		// Arrange

		var html = $"""<div><svg><{name}></{name}></svg></div>""";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be($"<svg>\n\t<{name}></{name}>\n</svg>");
	}

	[Fact]
	public void DropsWhitespaceOnlyTextNodes()
	{
		// Arrange

		var html = "<div><svg>\n\n        <defs></defs>\n\n        <g>\n            <path d=\"M0 0\"></path>\n        </g>\n\n    </svg></div>";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be("<svg>\n\t<defs></defs>\n\t<g>\n\t\t<path d=\"M0 0\" />\n\t</g>\n</svg>");
	}

	[Fact]
	public void ClosesOnItsOwnLineWhenContentBeginsWithAnElement()
	{
		// Arrange

		var html = """
			<div><svg><text class="title"><tspan font-weight="bold">Bold</tspan> start, <tspan font-style="italic">italic</tspan> end</text></svg></div>
			""";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be(
			"<svg>\n\t<text class=\"title\">"
			+ "\n\t\t<tspan font-weight=\"bold\">Bold</tspan> start, <tspan font-style=\"italic\">italic</tspan> end"
			+ "\n\t</text>\n</svg>");
	}

	[Fact]
	public void ClosesInlineWhenContentBeginsWithText()
	{
		// Arrange

		var html = """
			<div><svg><text class="x-title">x <tspan baseline-shift="sub">2</tspan></text><text class="x-break-label">1</text></svg></div>
			""";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be(
			"<svg>\n\t<text class=\"x-title\">x <tspan baseline-shift=\"sub\">2</tspan></text>"
			+ "\n\t<text class=\"x-break-label\">1</text>\n</svg>");
	}

	[Fact]
	public void TrimsContentTextOnlyAtTheContentEdges()
	{
		// Arrange

		var html = "<div><svg><text>\n    lead <tspan>a</tspan> middle <tspan>b</tspan> trail\n  </text></svg></div>";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be("<svg>\n\t<text>lead <tspan>a</tspan> middle <tspan>b</tspan> trail</text>\n</svg>");
	}

	[Fact]
	public void EscapesTextAndAttributeValuesAsXml()
	{
		// Arrange

		var html = """
			<div><svg><text class="title" data-note="a &amp; b &lt;c&gt;">end&apos;s &quot;quote&quot; &amp; &lt;amp&gt;</text></svg></div>
			""";

		// Act

		var svg = Extract(html);

		// Assert

		svg.Should().Be(
			"<svg>\n\t<text class=\"title\" data-note=\"a &amp; b &lt;c&gt;\">"
			+ "end&apos;s &quot;quote&quot; &amp; &lt;amp&gt;</text>\n</svg>");
	}
}
