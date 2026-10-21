using GGNet.Components;
using GGNet.Scales;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Headless.Tests;

public class RadarFillInheritanceTests
{
	private sealed record Vertex(double Axis, double Value, string Entity);

	private static readonly Vertex[] vertices =
	[
		new(1, 2, "A"), new(2, 4, "A"), new(3, 3, "A"),
		new(1, 5, "B"), new(2, 3, "B"), new(3, 4, "B")
	];

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task DefaultInheritanceUsesFillForPolygonsAndLegend(bool panel, bool conflictingColor)
	{
		// Arrange

		var context = PlotContext.Build(vertices, p => p.Axis, p => p.Value)
			.Scale_Fill_Discrete(p => p.Entity, ["red", "blue"]);
		if (conflictingColor)
		{
			context.Scale_Color_Discrete(p => p.Axis, ["cyan", "yellow", "magenta"]);
		}
		if (panel)
		{
			context.Default_Panel().Geom_Radar();
		}
		else
		{
			context.Geom_Radar();
		}

		// Act

		var svg = await context.Style().AsStringAsync();
		var polygons = Polygons(context);

		// Assert

		using var _ = new AssertionScope();
		polygons.Should().HaveCount(2);
		polygons.Select(p => p.Aesthetic.Fill).Should().Equal("red", "blue");
		polygons.Select(p => p.Aesthetic.Stroke).Should().Equal("red", "blue");
		polygons[0].Path.Longitude.Should().Equal(1, 2, 3);
		polygons[0].Path.Latitude.Should().Equal(2, 4, 3);
		polygons[1].Path.Longitude.Should().Equal(1, 2, 3);
		polygons[1].Path.Latitude.Should().Equal(5, 3, 4);
		context.Legends.Should().ContainSingle().Which.Aes.Should().BeSameAs(context.Aesthetics.Fill);
		XDocument.Parse(svg).Descendants().Where(e => (string?)e.Attribute("class") == "legend-label")
			.Select(e => e.Value).Should().BeEquivalentTo(["A", "B"]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ColorOnlyDoesNotProvideFallbackFill(bool panel)
	{
		// Arrange

		var context = PlotContext.Build(vertices, p => p.Axis, p => p.Value)
			.Scale_Color_Discrete(p => p.Entity, ["red", "blue"]);
		if (panel)
		{
			context.Default_Panel().Geom_Radar(fill: "green");
		}
		else
		{
			context.Geom_Radar(fill: "green");
		}

		// Act

		var svg = await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		var polygon = Polygons(context).Should().ContainSingle().Subject;
		polygon.Aesthetic.Fill.Should().Be("green");
		polygon.Path.Longitude.Should().HaveCount(vertices.Length);
		context.Legends.Should().BeEmpty();
		svg.Should().NotContain("legend-label");
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task ExplicitFillMappingWinsWithEitherInheritanceSetting(bool panel, bool inherit)
	{
		// Arrange

		var mapping = new ExplicitFill();
		var context = PlotContext.Build(vertices, p => p.Axis, p => p.Value)
			.Scale_Fill_Discrete(p => p.Entity, ["red", "blue"])
			.Scale_Color_Discrete(p => p.Entity, ["cyan", "yellow"]);
		if (panel)
		{
			context.Default_Panel().Geom_Radar(fillBy: mapping, inherit: inherit);
		}
		else
		{
			context.Geom_Radar(fillBy: mapping, inherit: inherit);
		}

		// Act

		await context.Style().AsStringAsync();

		// Assert

		using var _ = new AssertionScope();
		var polygon = Polygons(context).Should().ContainSingle().Subject;
		polygon.Aesthetic.Fill.Should().Be("purple");
		polygon.Path.Longitude.Should().HaveCount(vertices.Length);
		context.Legends.Should().BeEmpty();
	}

	[Fact]
	public async Task EqualMappedFillMergesEntitiesIntoOnePolygon()
	{
		// Arrange

		var context = PlotContext.Build(vertices, p => p.Axis, p => p.Value)
			.Scale_Fill_Discrete(p => p.Entity, ["red", "red"])
			.Geom_Radar().Style();

		// Act

		await context.AsStringAsync();

		// Assert

		var polygon = Polygons(context).Should().ContainSingle().Subject;
		polygon.Path.Longitude.Should().HaveCount(vertices.Length);
	}

	[Fact]
	public async Task VaryingContinuousFillGroupsVerticesAcrossEntitiesByMappedColor()
	{
		// Arrange

		var context = PlotContext.Build(vertices, p => p.Axis, p => p.Value)
			.Scale_Fill_Continuous(p => p.Axis, ["red", "green", "blue"])
			.Geom_Radar().Style();

		// Act

		await context.AsStringAsync();
		var polygons = Polygons(context);

		// Assert

		using var _ = new AssertionScope();
		polygons.Should().HaveCount(3);
		polygons.Select(p => p.Aesthetic.Fill).Should().Equal("red", "green", "blue");
		polygons[0].Path.Longitude.Should().Equal(1, 1);
		polygons[0].Path.Latitude.Should().BeEquivalentTo([2.0, 5.0]);
		polygons[1].Path.Longitude.Should().Equal(2, 2);
		polygons[2].Path.Longitude.Should().Equal(3, 3);
	}

	[Fact]
	public async Task ContinuousFillGroupsWithOneVertexDoNotProducePolygons()
	{
		// Arrange

		var context = PlotContext.Build(vertices[..3], p => p.Axis, p => p.Value)
			.Scale_Fill_Continuous(p => p.Axis, ["red", "green", "blue"])
			.Geom_Radar().Style();

		// Act

		await context.AsStringAsync();

		// Assert

		Polygons(context).Should().BeEmpty();
	}

	[Fact]
	public async Task VertexTooltipUsesInheritedFillRatherThanColor()
	{
		// Arrange

		RenderFragment content = builder => builder.AddContent(0, "vertex");
		var tooltip = new Mock<ITooltip>();
		var component = new Mock<IPanel>();
		component.Setup(p => p.Tooltip).Returns(tooltip.Object);
		var context = PlotContext.Build(vertices, p => p.Axis, p => p.Value)
			.Scale_Fill_Discrete(p => p.Entity, ["red", "blue"])
			.Scale_Color_Discrete(p => p.Entity, ["cyan", "yellow"])
			.Geom_Radar(tooltip: _ => content, fillOpacity: 0.4).Style();
		await context.AsStringAsync();
		context.Panels[0].Component = component.Object;
		var circles = context.Panels[0].Geoms[0].Layer.Select(p => p.Value).OfType<Shapes.Circle>().ToArray();

		// Act

		await circles[0].OnMouseOver!(new MouseEventArgs());
		await circles[3].OnMouseOver!(new MouseEventArgs());

		// Assert

		tooltip.Verify(t => t.Show(1, 2, 0, content, "red", 0.4), Times.Once);
		tooltip.Verify(t => t.Show(1, 5, 0, content, "blue", 0.4), Times.Once);
		tooltip.VerifyNoOtherCalls();
	}

	private static Shapes.Polygon[] Polygons(PlotContext<Vertex, double, double> context)
		=> context.Panels[0].Geoms[0].Layer.Select(p => p.Value).OfType<Shapes.Polygon>().ToArray();

	private sealed class ExplicitFill : IAestheticMapping<Vertex, string>
	{
		public bool Guide => false;

		public string? Name => null;

		public Guide Type => GGNet.Guide.None;

		public IEnumerable<(string value, string label)> Labels => [];

		public void Train(Vertex item) { }

		public string Map(Vertex item) => "purple";
	}
}
