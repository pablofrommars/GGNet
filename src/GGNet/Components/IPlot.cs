namespace GGNet.Components;

internal interface IPlot
{
	Task RefreshAsync(RenderTarget target = RenderTarget.Render, CancellationToken token = default);
}
