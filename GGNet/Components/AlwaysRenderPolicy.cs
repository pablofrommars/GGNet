namespace GGNet.Components;

public class AlwaysRenderPolicy : RenderPolicyBase
{
	public AlwaysRenderPolicy(IPlot plot)
		: base(plot)
	{
	}

	public override Task RefreshAsync()
	{
		plot.Render();

		return plot.StateHasChangedAsync();
	}

	public override bool ShouldRender() => true;

	public class RenderChildPolicy : RenderChildPolicyBase
	{
		public override bool ShouldRender() => true;
	}

	public override RenderChildPolicyBase Child() => new RenderChildPolicy();
}
