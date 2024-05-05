namespace GGNet.Rendering;

public interface IPlotRendering
{
	IRenderPolicy? Policy { get; }

	void Render(RenderTarget target);

	Task StateHasChangedAsync();
}
