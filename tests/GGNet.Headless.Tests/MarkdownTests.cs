namespace GGNet.Headless.Tests;

public class MarkdownTests
{
	[Fact]
	public void EncodesUnmatchedText()
	{
		// Arrange

		var markdown = "<script>alert(1)</script>";

		// Act

		var text = Markdown.Text(markdown);

		// Assert

		using var _ = new AssertionScope();

		text.Should().NotContain("<script");
		text.Should().Be("&lt;script&gt;alert(1)&lt;/script&gt;");
	}

	[Fact]
	public void EncodesTokenValues()
	{
		// Arrange

		var markdown = "**<img src=x onerror=\"alert(1)\">**";

		// Act

		var text = Markdown.Text(markdown);

		// Assert

		using var _ = new AssertionScope();

		text.Should().NotContain("<img");
		text.Should().NotContain("onerror=\"");
		text.Should().Be("<tspan font-weight=\"bold\">&lt;img src=x onerror=&quot;alert(1)&quot;&gt;</tspan>");
	}

	[Fact]
	public void EncodesTextAroundTokens()
	{
		// Arrange

		var markdown = "</text><a href='x'> **bold** <b>";

		// Act

		var text = Markdown.Text(markdown);

		// Assert

		using var _ = new AssertionScope();

		text.Should().NotContain("</text>");
		text.Should().NotContain("<a href");
		text.Should().Be("&lt;/text&gt;&lt;a href=&apos;x&apos;&gt; <tspan font-weight=\"bold\">bold</tspan> &lt;b&gt;");
	}

	[Fact]
	public void EncodesAmpersand()
	{
		// Arrange

		var markdown = "Ale & Lager ~&~";

		// Act

		var text = Markdown.Text(markdown);

		// Assert

		text.Should().Be("Ale &amp; Lager <tspan baseline-shift=\"sub\" font-size=\"0.7em\">&amp;</tspan>");
	}

	[Theory]
	[InlineData("**bold**", "<tspan font-weight=\"bold\">bold</tspan>")]
	[InlineData("*italic*", "<tspan font-style=\"italic\">italic</tspan>")]
	[InlineData("~sub~", "<tspan baseline-shift=\"sub\" font-size=\"0.7em\">sub</tspan>")]
	[InlineData("^sup^", "<tspan baseline-shift=\"super\" font-size=\"0.7em\">sup</tspan>")]
	[InlineData("plain — text", "plain — text")]
	public void PreservesStyling(string markdown, string expected)
	{
		// Arrange / Act

		var text = Markdown.Text(markdown);

		// Assert

		text.Should().Be(expected);
	}

	[Fact]
	public async Task HostileLabelsRenderInert()
	{
		// Arrange

		var payload = "</text><script>alert(1)</script>";

		var plot = PlotContext.Build([0, 1], o => o, o => o)
			  .Title(payload)
			  .SubTitle(payload)
			  .Caption(payload)
			  .XLab(payload)
			  .YLab(payload)
			  .Geom_Line()
			  .Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		svg.Should().NotContain("<script");
		svg.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");

		XDocument.Parse(svg).Descendants()
			.Should().NotContain(element => element.Name.LocalName == "script");
	}
}
