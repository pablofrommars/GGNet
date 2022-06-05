namespace GGNet.Components;

public interface IPlotRendering
{
	IRenderPolicy Policy { get; }

	void Render(RenderTarget target);

	Task StateHasChangedAsync();
}