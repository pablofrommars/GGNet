using GGNet.Exceptions;

using Microsoft.AspNetCore.Components;

namespace GGNet.Components.Tests;

public class InteractivityParameterTests : BunitContext
{
	private sealed record P(double X, double Y);

	private static PlotContext<P, double, double> Context(double start = 0)
		=> PlotContext.Build(new[] { new P(start, start), new P(start + 1, start + 1) }, p => p.X, p => p.Y)
			.Geom_Point().Style();

	private IRenderedComponent<Plot<P, double, double>> Mount(RenderMode mode, InteractivityOptions? options)
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		return Render<Plot<P, double, double>>(p => p.Add(x => x.Context, Context())
			.Add(x => x.RenderMode, mode).Add(x => x.Interactivity, options));
	}

	[Theory]
	[InlineData(RenderMode.InteractiveAuto, false)]
	[InlineData(RenderMode.InteractiveAuto, true)]
	[InlineData(RenderMode.Interactive, false)]
	[InlineData(RenderMode.Interactive, true)]
	[InlineData(RenderMode.Static, false)]
	[InlineData(RenderMode.Static, true)]
	public async Task ChangedOptInRejectsTheWholeParameterUpdate(RenderMode mode, bool enabled)
	{
		// Arrange

		var initial = enabled ? new InteractivityOptions { Pan = true } : null;
		var cut = Mount(mode, initial);
		var context = cut.Instance.Context;
		var markup = cut.Markup;
		var replacement = Context(100);
		var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
		{
			[nameof(Plot<,,>.Context)] = replacement,
			[nameof(Plot<,,>.Width)] = 1200.0,
			[nameof(Plot<,,>.Interactivity)] = enabled ? null : new InteractivityOptions()
		});

		// Act

		Func<Task> update = () => cut.InvokeAsync(() => cut.Instance.SetParametersAsync(parameters));

		// Assert

		await update.Should().ThrowAsync<GGNetUserException>()
			.WithMessage("*Interactivity*initialization-only*Recreate*Plot*@key*");
		using var _ = new AssertionScope();
		cut.Instance.Interactivity.Should().BeSameAs(initial);
		cut.Instance.Context.Should().BeSameAs(context);
		cut.Instance.Width.Should().Be(720);
		cut.Markup.Should().Be(markup);
	}

	public static IEnumerable<object[]> ChangedOptions()
	{
		yield return [new InteractivityOptions { Pan = true }];
		yield return [new InteractivityOptions { Zoom = ZoomAxis.Both }];
		yield return [new InteractivityOptions { ZoomStep = 0.5 }];
		yield return [new InteractivityOptions { AutoFitY = true }];
		yield return [new InteractivityOptions { Crosshair = true }];
		yield return [new InteractivityOptions { CursorTooltip = true }];
	}

	[Theory]
	[MemberData(nameof(ChangedOptions))]
	public async Task EveryOptionValueIsInitializationOnly(InteractivityOptions changed)
	{
		// Arrange

		var initial = new InteractivityOptions();
		var cut = Mount(RenderMode.InteractiveAuto, initial);
		var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
		{
			[nameof(Plot<,,>.Interactivity)] = changed
		});

		// Act

		Func<Task> update = () => cut.InvokeAsync(() => cut.Instance.SetParametersAsync(parameters));

		// Assert

		await update.Should().ThrowAsync<GGNetUserException>();
		cut.Instance.Interactivity.Should().BeSameAs(initial);
	}

	[Theory]
	[InlineData(RenderMode.InteractiveAuto, false)]
	[InlineData(RenderMode.InteractiveAuto, true)]
	[InlineData(RenderMode.Interactive, false)]
	[InlineData(RenderMode.Interactive, true)]
	[InlineData(RenderMode.Static, false)]
	[InlineData(RenderMode.Static, true)]
	public async Task EqualValuesAndOmittedOptionsRemainValid(RenderMode mode, bool enabled)
	{
		// Arrange

		var initial = enabled ? new InteractivityOptions { Pan = true, CursorTooltip = true } : null;
		var equal = initial is null ? null : initial with { };
		var cut = Mount(mode, initial);
		var options = ParameterView.FromDictionary(new Dictionary<string, object?>
		{
			[nameof(Plot<,,>.Interactivity)] = equal
		});
		var dimensions = ParameterView.FromDictionary(new Dictionary<string, object?>
		{
			[nameof(Plot<,,>.Width)] = 1200.0
		});

		// Act

		await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(options));
		await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(dimensions));

		// Assert

		using var _ = new AssertionScope();
		cut.Instance.Interactivity.Should().BeSameAs(equal);
		cut.Instance.Width.Should().Be(1200);
		JSInterop.Invocations.Count(i => i.Identifier == "import").Should().Be(enabled ? 1 : 0);
	}

	[Fact]
	public async Task ParameterNameCasingCannotBypassTheGuard()
	{
		// Arrange

		var cut = Mount(RenderMode.InteractiveAuto, null);
		var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
		{
			["interactivity"] = new InteractivityOptions { Pan = true }
		});

		// Act

		Func<Task> update = () => cut.InvokeAsync(() => cut.Instance.SetParametersAsync(parameters));

		// Assert

		await update.Should().ThrowAsync<GGNetUserException>();
		cut.Instance.Interactivity.Should().BeNull();
	}

	[Fact]
	public void RecreatingThePlotAllowsDifferentOptions()
	{
		// Arrange

		var original = Mount(RenderMode.InteractiveAuto, null);
		var options = new InteractivityOptions { Pan = true };

		// Act

		var replacement = Mount(RenderMode.InteractiveAuto, options);

		// Assert

		using var _ = new AssertionScope();
		original.Instance.Interactivity.Should().BeNull();
		replacement.Instance.Interactivity.Should().BeSameAs(options);
		JSInterop.Invocations.Count(i => i.Identifier == "import").Should().Be(1);
	}
}
