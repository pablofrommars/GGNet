namespace GGNet.Components;

public abstract class RenderPolicyBase : IAsyncDisposable
{
	protected readonly IPlot plot;

	public RenderPolicyBase(IPlot plot)
	{
		this.plot = plot;
	}

	public virtual Task RefreshAsync() => Task.CompletedTask;

	public virtual bool ShouldRender() => false;

	public virtual void OnAfterRender(bool firstRender) { }

	public virtual ValueTask DisposeAsync() => default;

	public abstract RenderChildPolicyBase Child();

	public static RenderPolicyBase Factory(RenderPolicy policy, IPlot component) => policy switch
	{
		RenderPolicy.Always => new AlwaysRenderPolicy(component),
		RenderPolicy.Never => new NeverRenderPolicy(component),
		_ => new AutoRenderPolicy(component),
	};
}
