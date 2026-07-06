using GGNet.Components;
using GGNet.Layout;

namespace GGNet.Headless.Tests;

// Gutter carving as unit tests: the panel's zone arithmetic, previously only
// observable through rendered SVG, asserted directly.
public class PanelLayoutTests
{
	private static readonly Zone outer = new() { X = 10, Y = 20, Width = 300, Height = 200 };

	private static PanelLayout.Inputs Inputs(
		Style? style = null,
		string? xStrip = null,
		string? yStrip = null,
		bool xAxis = false,
		bool yAxis = false,
		bool carveAxes = true,
		double axisWidth = 0,
		double axisHeight = 0,
		double yLabWidth = 0,
		double xLabHeight = 0,
		bool hasXTitles = false,
		bool hasYTitles = false)
		=> new(
			Outer: outer,
			XStripText: xStrip,
			YStripText: yStrip,
			XAxis: xAxis,
			YAxis: yAxis,
			CarveAxes: carveAxes,
			AxisWidth: axisWidth,
			AxisHeight: axisHeight,
			YLabWidth: yLabWidth,
			XLabHeight: xLabHeight,
			HasXTitles: hasXTitles,
			HasYTitles: hasYTitles,
			Style: style ?? Style.Default());

	[Fact]
	public void NothingToCarveLeavesTheOuterZone()
	{
		// Arrange

		// Act

		var zones = PanelLayout.Compute(Inputs());

		// Assert

		zones.Area.Should().Be(outer);
	}

	[Fact]
	public void PolarSkipsAxisBands()
	{
		// Arrange

		// Act

		var zones = PanelLayout.Compute(Inputs(xAxis: true, yAxis: true, carveAxes: false, axisWidth: 30, axisHeight: 12, xLabHeight: 10));

		// Assert

		using var _ = new AssertionScope();

		zones.Area.Should().Be(outer);
		zones.XAxisText.Width.Should().Be(0);
		zones.YAxisText.Width.Should().Be(0);
	}

	[Fact]
	public void LeftAxisCarvesFromTheLeft()
	{
		// Arrange

		var style = Style.Default();
		var textBand = style.Axis.Text.Y.Margin.Left + 30 + style.Axis.Text.Y.Margin.Right;

		// Act

		var zones = PanelLayout.Compute(Inputs(style: style, yAxis: true, axisWidth: 30));

		// Assert

		using var _ = new AssertionScope();

		zones.YAxisText.X.Should().Be(outer.X + style.Axis.Text.Y.Margin.Left + 30);
		zones.YAxisText.Y.Should().Be(outer.Y);
		zones.YAxisText.Width.Should().Be(textBand);
		zones.YAxisText.Height.Should().Be(outer.Height);

		zones.Area.X.Should().Be(outer.X + textBand);
		zones.Area.Width.Should().Be(outer.Width - textBand);
		zones.Area.Height.Should().Be(outer.Height);

		zones.YAxisTitle.Width.Should().Be(0);
	}

	[Fact]
	public void RightAxisCarvesFromTheRight()
	{
		// Arrange

		var style = Style.Default(axisY: GGNet.Position.Right);
		var textBand = style.Axis.Text.Y.Margin.Left + 30 + style.Axis.Text.Y.Margin.Right;

		// Act

		var zones = PanelLayout.Compute(Inputs(style: style, yAxis: true, axisWidth: 30));

		// Assert

		using var _ = new AssertionScope();

		zones.YAxisText.X.Should().Be(outer.X + outer.Width - 30);
		zones.Area.X.Should().Be(outer.X);
		zones.Area.Width.Should().Be(outer.Width - textBand);
	}

	[Fact]
	public void YLabAddsATitleBand()
	{
		// Arrange

		var style = Style.Default();
		var titleBand = style.Axis.Title.Y.Margin.Left + 15 + style.Axis.Title.Y.Margin.Right;
		var textBand = style.Axis.Text.Y.Margin.Left + 30 + style.Axis.Text.Y.Margin.Right;

		// Act

		var zones = PanelLayout.Compute(Inputs(style: style, yAxis: true, axisWidth: 30, yLabWidth: 15));

		// Assert

		using var _ = new AssertionScope();

		zones.YAxisTitle.X.Should().Be(outer.X + style.Axis.Title.Y.Margin.Left + 15);
		zones.YAxisTitle.Width.Should().Be(titleBand);

		zones.Area.X.Should().Be(outer.X + titleBand + textBand);
		zones.Area.Width.Should().Be(outer.Width - titleBand - textBand);
	}

	[Fact]
	public void XAxisCarvesFromTheBottom()
	{
		// Arrange

		var style = Style.Default();
		var textBand = style.Axis.Text.X.Margin.Top + 12 + style.Axis.Text.X.Margin.Bottom;
		var titleBand = style.Axis.Title.X.Margin.Top + 10 + style.Axis.Title.X.Margin.Bottom;

		// Act

		var zones = PanelLayout.Compute(Inputs(style: style, xAxis: true, axisHeight: 12, xLabHeight: 10));

		// Assert

		using var _ = new AssertionScope();

		zones.XAxisTitle.Y.Should().Be(outer.Y + outer.Height - style.Axis.Title.X.Margin.Bottom);
		zones.XAxisText.Y.Should().Be(outer.Y + outer.Height - titleBand - style.Axis.Text.X.Margin.Bottom);

		zones.Area.Height.Should().Be(outer.Height - titleBand - textBand);
		zones.Area.Y.Should().Be(outer.Y);
	}

	[Fact]
	public void XStripCarvesTheTopAndAlignsToTheFinalArea()
	{
		// Arrange

		var style = Style.Default();
		var stripHeight = style.Strip.Text.X.Margin.Top + "strip".Height(style.Strip.Text.X.FontSize) + style.Strip.Text.X.Margin.Bottom;
		var textBand = style.Axis.Text.Y.Margin.Left + 30 + style.Axis.Text.Y.Margin.Right;

		// Act

		var zones = PanelLayout.Compute(Inputs(style: style, xStrip: "strip", yAxis: true, axisWidth: 30));

		// Assert

		using var _ = new AssertionScope();

		zones.Area.Y.Should().Be(outer.Y + stripHeight);
		zones.Area.Height.Should().Be(outer.Height - stripHeight);

		// The strip label aligns to the area left over after axis carving.
		zones.XStrip.X.Should().Be(outer.X + textBand + style.Strip.Text.X.Margin.Left);
	}

	[Fact]
	public void YStripCarvesTheRight()
	{
		// Arrange

		var style = Style.Default();
		var stripWidth = style.Strip.Text.Y.Margin.Left + "s".Height(style.Strip.Text.Y.FontSize) + style.Strip.Text.Y.Margin.Right;

		// Act

		var zones = PanelLayout.Compute(Inputs(style: style, yStrip: "s"));

		// Assert

		using var _ = new AssertionScope();

		zones.Area.Width.Should().Be(outer.Width - stripWidth);
		zones.YStrip.Y.Should().Be(outer.Y + style.Strip.Text.Y.Margin.Top);
	}
}
