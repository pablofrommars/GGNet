namespace GGNet.Rendering;

public interface IRenderPolicy : IAsyncDisposable
{
	Task RefreshAsync(RenderTarget target, CancellationToken token);

	bool ShouldRender();

	void OnAfterRender(bool firstRender);

	IChildRenderPolicy Child();
}
