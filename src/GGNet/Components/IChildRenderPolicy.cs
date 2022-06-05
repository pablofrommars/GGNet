namespace GGNet.Components;

public interface IChildRenderPolicy
{
	void Refresh(RenderTarget target = RenderTarget.All);

	bool ShouldRender(RenderTarget target = RenderTarget.All);
}