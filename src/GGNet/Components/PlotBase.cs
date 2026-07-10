using Microsoft.Extensions.Logging;

namespace GGNet.Components;

using Rendering;

public abstract class PlotBase<T, TX, TY> : ComponentBase, IPlot, IPlotRendering, IAsyncDisposable
  where TX : struct
  where TY : struct
{
	[Parameter]
	public required PlotContext<T, TX, TY> Context { get; init; }

	[Parameter]
	public required RenderMode RenderMode { get; init; }

	// IServiceProvider, not ILoggerFactory: the headless renderer builds an
	// empty container, so a hard logging dependency would break static renders.
	[Inject]
	private IServiceProvider Services { get; init; } = default!;

	public string Id => Context.Id;

	public Style Style => Context.Style!;

	protected override void OnInitialized()
	{
		var logger = (Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory)?.CreateLogger("GGNet.Plot");

		RenderModeHandler = Rendering.RenderModeHandler.Factory(RenderMode, this, logger);
	}

	public IRenderModeHandler? RenderModeHandler { get; set; }

	public abstract void Render(RenderTarget target);

	public Task StateHasChangedAsync() => InvokeAsync(StateHasChanged);

	protected override bool ShouldRender() => RenderModeHandler?.ShouldRender() ?? true;

	protected override void OnAfterRender(bool firstRender) => RenderModeHandler?.OnAfterRender(firstRender);

	public Task RefreshAsync(RenderTarget target, CancellationToken token)
	{
		if (RenderModeHandler is null)
		{
			return Task.CompletedTask;
		}

		return RenderModeHandler.RefreshAsync(target, token);
	}

	private int disposing;

	public ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
		{
			return ValueTask.CompletedTask;
		}

		if (RenderModeHandler is null)
		{
			return ValueTask.CompletedTask;
		}

		return RenderModeHandler.DisposeAsync();
	}
}
