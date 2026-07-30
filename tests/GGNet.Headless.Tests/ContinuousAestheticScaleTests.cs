using GGNet.Scales;

namespace GGNet.Headless.Tests;

// The continuous fill/size scales used to treat (0, 0) as "untrained", so a leading zero
// observation was erased by the next value, and a genuinely constant range produced an
// unmapped fill (mark skipped) or a NaN radius (invalid SVG).
public class ContinuousAestheticScaleTests
{
	private static readonly string[] palette = ["#000000", "#444444", "#888888", "#bbbbbb", "#ffffff"];

	[Fact]
	public void FillRetainsLeadingZero()
	{
		// Arrange

		var sut = new FillContinuous(palette);

		// Act

		sut.Train(0.0);
		sut.Train(10.0);

		// Assert

		using var _ = new AssertionScope();

		sut.Map(0.0).Should().Be("#000000");
		sut.Map(10.0).Should().Be("#ffffff");
	}

	[Fact]
	public void FillConstantRangeMapsToMiddleColor()
	{
		// Arrange

		var sut = new FillContinuous(palette);

		// Act

		sut.Train(7.0);
		sut.Train(7.0);

		// Assert

		sut.Map(7.0).Should().Be("#888888");
	}

	[Fact]
	public void FillClearRestoresUntrainedState()
	{
		// Arrange

		var sut = new FillContinuous(palette);
		sut.Train(100.0);

		// Act

		sut.Clear();
		sut.Train(0.0);
		sut.Train(10.0);

		// Assert

		using var _ = new AssertionScope();

		sut.Map(0.0).Should().Be("#000000");
		sut.Map(10.0).Should().Be("#ffffff");
	}

	[Fact]
	public void SizeRetainsLeadingZero()
	{
		// Arrange

		var sut = new SizeContinuous(range: (1.0, 10.0));

		// Act

		sut.Train(0.0);
		sut.Train(100.0);

		// Assert

		using var _ = new AssertionScope();

		sut.Map(0.0).Should().Be(1.0);
		sut.Map(100.0).Should().Be(10.0);
	}

	[Fact]
	public void SizeConstantRangeMapsToMidRange()
	{
		// Arrange

		var sut = new SizeContinuous(range: (2.0, 10.0));

		// Act

		sut.Train(5.0);
		sut.Train(5.0);

		// Assert

		using var _ = new AssertionScope();

		sut.Map(5.0).Should().NotBe(double.NaN);
		sut.Map(5.0).Should().Be(6.0);
	}

	[Fact]
	public void SizeClearRestoresUntrainedState()
	{
		// Arrange

		var sut = new SizeContinuous(range: (1.0, 10.0));
		sut.Train(100.0);

		// Act

		sut.Clear();
		sut.Train(0.0);
		sut.Train(100.0);

		// Assert

		sut.Map(0.0).Should().Be(1.0);
	}

	private sealed record XYV(double X, double Y, double Value);

	private static readonly XYV[] constant =
	[
		new(1.0, 1.0, 3.0),
		new(2.0, 2.0, 3.0),
		new(3.0, 3.0, 3.0)
	];

	[Fact]
	public async Task ConstantFillLayerRenders()
	{
		// Arrange

		var plot = PlotContext.Build(constant, i => i.X, i => i.Y)
			.Scale_Fill_Continuous(i => i.Value, palette)
			.Geom_Bar()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		svg.Should().Contain("fill=\"#888888\"");
	}

	[Fact]
	public async Task ConstantSizeLayerEmitsFiniteRadii()
	{
		// Arrange

		var plot = PlotContext.Build(constant, i => i.X, i => i.Y)
			.Scale_Size_Continuous(i => i.Value, range: (2.0, 10.0))
			.Geom_Point()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		using var _ = new AssertionScope();

		svg.Should().NotContain("NaN");
		svg.Should().Contain("r=\"6\"");
	}
}
