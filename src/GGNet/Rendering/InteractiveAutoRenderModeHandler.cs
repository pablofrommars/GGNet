namespace GGNet.Rendering;

internal sealed class InteractiveAutoRenderModeHandler(IPlotRendering plot) : RenderModeHandler(plot)
{
	public override async Task RefreshAsync(RenderTarget target, CancellationToken token)
	{
		plot.Render(RenderTarget.Render);

		// A refresh must surface without waiting for an unrelated event render:
		// host commands (ZoomToXAsync, ResetViewAsync) arrive outside Blazor's
		// event pipeline, so nothing else schedules the render.
		await plot.StateHasChangedAsync();
	}

	public override bool ShouldRender() => true;

	public sealed class ChildRenderHandler : IChildRenderModeHandler
	{
		public void Refresh()
		{
		}

		public bool ShouldRender() => true;
	}

	public override IChildRenderModeHandler Child() => new ChildRenderHandler();
}
