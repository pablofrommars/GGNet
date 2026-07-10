namespace GGNet.Rendering;

internal interface IPlotRendering
{
	IRenderModeHandler? RenderModeHandler { get; }

	void Render(RenderTarget target);

	Task StateHasChangedAsync();
}
