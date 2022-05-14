namespace GGNet.Components;

public sealed class AlwaysRenderPolicy : RenderPolicyBase
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

	public sealed class RenderChildPolicy : RenderChildPolicyBase
	{
		public override bool ShouldRender() => true;
	}

	public override RenderChildPolicyBase Child() => new RenderChildPolicy();
}