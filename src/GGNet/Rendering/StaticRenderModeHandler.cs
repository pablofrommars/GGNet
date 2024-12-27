namespace GGNet.Rendering;

public sealed class StaticRenderModeHandler(IPlotRendering plot) : RenderModeHandler(plot)
{
    public sealed class ChildRenderHandler : IChildRenderModeHandler
	{
		public void Refresh(RenderTarget target = RenderTarget.All)
		{
		}

		public bool ShouldRender(RenderTarget target = RenderTarget.All)
			=> false;
	}

	public override IChildRenderModeHandler Child() => new ChildRenderHandler();
}
