namespace GGNet.Rendering;

public interface IRenderModeHandler : IAsyncDisposable
{
	Task RefreshAsync(RenderTarget target, CancellationToken token);

	bool ShouldRender();

	void OnAfterRender(bool firstRender);

	IChildRenderModeHandler Child();
}
