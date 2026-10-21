namespace GGNet.Components.Tests;

public class PreviewClippingTests : BunitContext
{
	private sealed record P(double X, double Y, string Group);

	[Fact]
	public void EachFacetKeepsThePreviewInsideItsOwnUntransformedClip()
	{
		// Arrange

		JSInterop.Mode = JSRuntimeMode.Loose;
		P[] data = [new(1, 2, "a"), new(2, 3, "a"), new(3, 4, "b"), new(4, 5, "b")];
		var context = PlotContext.Build(data, p => p.X, p => p.Y)
			.Geom_Point()
			.Facet_Wrap(p => p.Group)
			.Style();

		// Act

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto)
			.Add(p => p.Interactivity, new InteractivityOptions { Pan = true }));

		// Assert

		using var _ = new AssertionScope();

		var targets = cut.FindAll("[data-ggnet-pan]");
		targets.Should().HaveCount(2);
		targets.Select(target => target.ParentElement!.GetAttribute("clip-path")).Should().OnlyHaveUniqueItems();

		foreach (var target in targets)
		{
			var outer = target.ParentElement!;
			var inner = target.FirstElementChild!;
			var clip = outer.GetAttribute("clip-path")!;
			var clipId = clip[5..^1];
			var bounds = cut.Find($"clipPath[id='{clipId}'] rect");
			var panel = outer.ParentElement!.QuerySelector("rect.panel")!;

			outer.TagName.Should().Be("g");
			outer.HasAttribute("transform").Should().BeFalse();
			target.HasAttribute("clip-path").Should().BeFalse();
			inner.GetAttribute("clip-path").Should().Be(clip);
			outer.QuerySelector("rect.panel").Should().BeNull();

			foreach (var attribute in new[] { "x", "y", "width", "height" })
			{
				bounds.GetAttribute(attribute).Should().Be(panel.GetAttribute(attribute));
			}
		}
	}

	[Fact]
	public void WithoutInteractivityTheMarksKeepOnlyTheExistingAreaClip()
	{
		// Arrange

		P[] data = [new(1, 2, "a"), new(2, 3, "a")];
		var context = PlotContext.Build(data, p => p.X, p => p.Y).Geom_Point().Style();

		// Act

		var cut = Render<Plot<P, double, double>>(parameters => parameters
			.Add(p => p.Context, context)
			.Add(p => p.RenderMode, RenderMode.InteractiveAuto));

		// Assert

		using var _ = new AssertionScope();

		cut.FindAll("[data-ggnet-pan]").Should().BeEmpty();
		cut.FindAll("svg > g[clip-path]").Should().ContainSingle();
		JSInterop.Invocations.Should().BeEmpty();
	}
}
