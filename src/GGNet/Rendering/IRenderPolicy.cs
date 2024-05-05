namespace GGNet.Rendering;

public interface IRenderPolicy : IAsyncDisposable
{
	Task RefreshAsync(RenderTarget target);

	bool ShouldRender();

	void OnAfterRender(bool firstRender);

	IChildRenderPolicy Child();
}
