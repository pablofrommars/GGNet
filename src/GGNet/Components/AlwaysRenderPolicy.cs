namespace GGNet.Components;

public sealed class AlwaysRenderPolicy : RenderPolicyBase
{
	public AlwaysRenderPolicy(IPlotRendering plot)
		: base(plot)
	{
	}

	public override Task RefreshAsync(RenderTarget target)
	{
		plot.Render(RenderTarget.All);

		//return plot.StateHasChangedAsync();
		return Task.CompletedTask;
	}

	public override bool ShouldRender() => true;

	public sealed class ChildRenderPolicy : IChildRenderPolicy
	{
		public void Refresh(RenderTarget target = RenderTarget.All)
		{
		}

		public bool ShouldRender(RenderTarget target) => true;
	}

	public override IChildRenderPolicy Child() => new ChildRenderPolicy();
}