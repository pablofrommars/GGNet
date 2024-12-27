namespace GGNet.Rendering;

public interface IChildRenderModeHandler
{
	void Refresh(RenderTarget target = RenderTarget.All);

	bool ShouldRender(RenderTarget target = RenderTarget.All);
}
