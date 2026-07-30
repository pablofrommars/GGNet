namespace GGNet.Headless.Tests;

// A palette that is empty or too short used to fail silently (every mapping left at default,
// so geoms skipped every mark) or as an IndexOutOfRangeException from inside a render pass.
// Both are configuration mistakes and now surface as GGNetUserException.
public class PaletteValidationTests
{
	private sealed record Reading(double X, double Y, string Tank);

	private static readonly Reading[] readings =
	[
		new(1.0, 2.0, "a"),
		new(2.0, 3.0, "b"),
		new(3.0, 4.0, "c")
	];

	[Fact]
	public async Task DiscretePaletteExhaustionThrows()
	{
		// Arrange

		// Three distinct tanks, two colors.
		var plot = PlotContext.Build(readings, i => i.X, i => i.Y)
			.Scale_Color_Discrete(i => i.Tank, ["#111111", "#222222"])
			.Geom_Point()
			.Style();

		// Act

		Func<Task> act = () => plot.AsStringAsync();

		// Assert

		(await act.Should().ThrowAsync<GGNetUserException>())
			.Which.Message.Should().Contain("3").And.Contain("2");
	}

	[Fact]
	public async Task SufficientDiscretePaletteRenders()
	{
		// Arrange

		var plot = PlotContext.Build(readings, i => i.X, i => i.Y)
			.Scale_Color_Discrete(i => i.Tank, ["#111111", "#222222", "#333333"])
			.Geom_Point()
			.Style();

		// Act

		var svg = await plot.AsStringAsync();

		// Assert

		svg.Should().Contain("#333333");
	}

	[Fact]
	public void EmptyDiscretePaletteThrowsAtTheSurface()
	{
		// Arrange

		var context = PlotContext.Build(readings, i => i.X, i => i.Y);

		// Act

		Action act = () => context.Scale_Color_Discrete(i => i.Tank, []);

		// Assert

		act.Should().Throw<GGNetUserException>()
			.Which.Message.Should().Contain("empty");
	}

	[Fact]
	public void EmptyFillPaletteThrowsAtTheSurface()
	{
		// Arrange

		var context = PlotContext.Build(readings, i => i.X, i => i.Y);

		// Act

		Action act = () => context.Scale_Fill_Continuous(i => i.Y, []);

		// Assert

		act.Should().Throw<GGNetUserException>()
			.Which.Message.Should().Contain("Scale_Fill_Continuous");
	}

	[Fact]
	public void EmptyPaletteThrowsBeforeAnyRender()
	{
		// Arrange / Act

		Action act = () => _ = new Palettes.Discrete<string, string>([]);

		// Assert

		act.Should().Throw<GGNetUserException>();
	}
}
