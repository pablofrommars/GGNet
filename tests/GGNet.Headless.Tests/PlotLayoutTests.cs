using GGNet.Components;
using GGNet.Layout;

namespace GGNet.Headless.Tests;

// Plot-level zone carving and panel subdivision as unit tests.
public class PlotLayoutTests
{
	private static PlotLayout.Inputs Inputs(
		Style? style = null,
		string? title = null,
		string? subTitle = null,
		string? caption = null,
		(double width, double height)? legends = null)
		=> new(
			Width: 720,
			Height: 576,
			Title: title,
			SubTitle: subTitle,
			Caption: caption,
			HasLegends: legends is not null,
			LegendsDimension: legends ?? default,
			Style: style ?? Style.Default());

	[Fact]
	public void EmptyPlotWrapsTheWholeCanvas()
	{
		// Arrange

		// Act

		var zones = PlotLayout.Compute(Inputs());

		// Assert

		zones.Wrapper.Should().Be(new Zone { X = 0, Y = 0, Width = 720, Height = 576 });
	}

	[Fact]
	public void TitleCarvesTheTop()
	{
		// Arrange

		var style = Style.Default();
		var height = "Title".Height(style.Plot.Title.FontSize);
		var band = style.Plot.Title.Margin.Top + height + style.Plot.Title.Margin.Bottom;

		// Act

		var zones = PlotLayout.Compute(Inputs(style: style, title: "Title"));

		// Assert

		using var _ = new AssertionScope();

		zones.Title.Y.Should().Be(style.Plot.Title.Margin.Top + height);
		zones.Wrapper.Y.Should().Be(band);
		zones.Wrapper.Height.Should().Be(576 - band);
	}

	[Fact]
	public void CaptionPinsToTheCarvedBottomRight()
	{
		// Arrange

		var style = Style.Default();

		// Act

		var zones = PlotLayout.Compute(Inputs(style: style, caption: "Caption", legends: (40, 100)));

		// Assert

		using var _ = new AssertionScope();

		zones.Caption.Y.Should().Be(576 - style.Plot.Caption.Margin.Bottom);

		// X pins to the wrapper after the right-hand legend carve.
		zones.Caption.X.Should().Be(zones.Wrapper.X + zones.Wrapper.Width - style.Plot.Caption.Margin.Right);
	}

	[Fact]
	public void LegendRightNarrowsTheWrapper()
	{
		// Arrange

		var style = Style.Default();
		var band = style.Legend.Margin.Left + 40 + style.Legend.Margin.Right;

		// Act

		var zones = PlotLayout.Compute(Inputs(style: style, legends: (40, 100)));

		// Assert

		using var _ = new AssertionScope();

		zones.Legend.X.Should().Be(720 - 40 - style.Legend.Margin.Right);
		zones.Wrapper.X.Should().Be(0);
		zones.Wrapper.Width.Should().Be(720 - band);
	}

	[Fact]
	public void LegendLeftShiftsTheWrapper()
	{
		// Arrange

		var style = Style.Default(legend: GGNet.Position.Left);
		var band = style.Legend.Margin.Left + 40 + style.Legend.Margin.Right;

		// Act

		var zones = PlotLayout.Compute(Inputs(style: style, legends: (40, 100)));

		// Assert

		using var _ = new AssertionScope();

		zones.Wrapper.X.Should().Be(band);
		zones.Wrapper.Width.Should().Be(720 - band);
	}

	[Fact]
	public void ZeroSizedLegendsCarveNothing()
	{
		// Arrange

		// Act

		var zones = PlotLayout.Compute(Inputs(legends: (0, 0)));

		// Assert

		zones.Wrapper.Width.Should().Be(720);
	}

	private static PlotLayout.SubdivisionInputs Subdivision(
		Style? style = null,
		(int rows, int cols)? n = null,
		double strip = 0,
		(double width, double height)? axis = null,
		(bool x, bool y)? axisVisibility = null)
		=> new(
			Wrapper: new Zone { X = 10, Y = 20, Width = 700, Height = 500 },
			N: n ?? (1, 1),
			Strip: strip,
			Axis: axis ?? (0, 0),
			AxisVisibility: axisVisibility ?? (true, true),
			AxisTitles: (0, 0),
			AxisTitlesVisibility: (false, false),
			HasXLab: false,
			Style: style ?? Style.Default());

	[Fact]
	public void SinglePanelFillsTheWrapper()
	{
		// Arrange

		// Act

		var zones = PlotLayout.Subdivide(Subdivision(), [(0, 0, 1.0, 1.0)]);

		// Assert

		zones.Should().Equal(new Zone { X = 10, Y = 20, Width = 700, Height = 500 });
	}

	[Fact]
	public void GridSplitsWithSpacing()
	{
		// Arrange

		var style = Style.Default();
		var w = (700 - style.Panel.Spacing.X) / 2.0;
		var h = (500 - style.Panel.Spacing.Y) / 2.0;

		// Act

		var zones = PlotLayout.Subdivide(Subdivision(style: style, n: (2, 2)), [
			(0, 0, 0.5, 0.5), (0, 1, 0.5, 0.5),
			(1, 0, 0.5, 0.5), (1, 1, 0.5, 0.5)
		]);

		// Assert

		using var _ = new AssertionScope();

		zones[0].Should().Be(new Zone { X = 10, Y = 20, Width = w, Height = h });
		zones[1].Should().Be(new Zone { X = 10 + w + style.Panel.Spacing.X, Y = 20, Width = w, Height = h });
		zones[2].Should().Be(new Zone { X = 10, Y = 20 + h + style.Panel.Spacing.Y, Width = w, Height = h });
		zones[3].Should().Be(new Zone { X = 10 + w + style.Panel.Spacing.X, Y = 20 + h + style.Panel.Spacing.Y, Width = w, Height = h });
	}

	[Fact]
	public void SharedAxisBandsGoToEdgePanels()
	{
		// Arrange

		// Shared axes (not free): the y-axis band belongs to column 0, the
		// x-axis band to the last row.
		var style = Style.Default();
		var axis = (width: 30.0, height: 12.0);

		// Act

		var zones = PlotLayout.Subdivide(
			Subdivision(style: style, n: (2, 2), axis: axis, axisVisibility: (false, false)), [
			(0, 0, 0.5, 0.5), (0, 1, 0.5, 0.5),
			(1, 0, 0.5, 0.5), (1, 1, 0.5, 0.5)
		]);

		// Assert

		using var _ = new AssertionScope();

		(zones[0].Width - zones[1].Width).Should().Be(30.0);
		(zones[2].Height - zones[0].Height).Should().Be(12.0);
	}

	[Fact]
	public void StripHeightGoesToTheFirstRow()
	{
		// Arrange

		// Act

		var zones = PlotLayout.Subdivide(Subdivision(n: (2, 1), strip: 16), [
			(0, 0, 1.0, 0.5),
			(1, 0, 1.0, 0.5)
		]);

		// Assert

		(zones[0].Height - zones[1].Height).Should().Be(16.0);
	}
}
