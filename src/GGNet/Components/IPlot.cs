namespace GGNet.Components;

internal interface IPlot
{
	Task RefreshAsync(RenderTarget target = RenderTarget.All, CancellationToken token = default);
}
