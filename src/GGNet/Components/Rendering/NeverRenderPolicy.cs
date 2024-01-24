namespace GGNet.Components.Rendering;

public sealed class NeverRenderPolicy(IPlotRendering plot) : RenderPolicyBase(plot)
{
    public sealed class ChildRenderPolicy : IChildRenderPolicy
	{
		public void Refresh(RenderTarget target = RenderTarget.All)
		{
		}

		public bool ShouldRender(RenderTarget target = RenderTarget.All)
			=> false;
	}

	public override IChildRenderPolicy Child() => new ChildRenderPolicy();
}
