namespace GGNet.Rendering;

public interface IPlotRendering
{
	IRenderModeHandler? RenderModeHandler { get; }

	void Render(RenderTarget target);

	Task StateHasChangedAsync();
}
