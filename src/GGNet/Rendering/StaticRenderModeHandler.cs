namespace GGNet.Rendering;

internal sealed class StaticRenderModeHandler(IPlotRendering plot) : RenderModeHandler(plot)
{
	public sealed class ChildRenderHandler : IChildRenderModeHandler
	{
		public void Refresh()
		{
		}

		public bool ShouldRender() => false;
	}

	public override IChildRenderModeHandler Child() => new ChildRenderHandler();
}
