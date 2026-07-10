using GGNet.Exceptions;

using Microsoft.Extensions.Logging;

namespace GGNet.Rendering;

internal abstract class RenderModeHandler(IPlotRendering plot) : IRenderModeHandler
{
	protected readonly IPlotRendering plot = plot;

	public virtual Task RefreshAsync(RenderTarget target, CancellationToken token)
		=> Task.CompletedTask;

	public virtual bool ShouldRender() => false;

	public virtual void OnAfterRender(bool firstRender) { }

	public abstract IChildRenderModeHandler Child();

	public virtual ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		return ValueTask.CompletedTask;
	}

	public static IRenderModeHandler Factory(RenderMode mode, IPlotRendering component, ILogger? logger = null)
		=> mode switch
		{
			RenderMode.Interactive => new InteractiveRenderModeHandler(component, logger),
			RenderMode.InteractiveAuto => new InteractiveAutoRenderModeHandler(component),
			RenderMode.Static => new StaticRenderModeHandler(component),
			_ => throw new GGNetInternalException("Not Implemented")
		};
}
