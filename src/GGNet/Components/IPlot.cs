namespace GGNet.Components;

public interface IPlot
{
	Task RefreshAsync(RenderTarget target = RenderTarget.All);
}