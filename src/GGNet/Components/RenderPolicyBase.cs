using GGNet.Exceptions;

namespace GGNet.Components;

public abstract class RenderPolicyBase : IRenderPolicy
{
	protected readonly IPlotRendering plot;

	public RenderPolicyBase(IPlotRendering plot)
	{
		this.plot = plot;
	}

	public virtual Task RefreshAsync(RenderTarget target)
		=> Task.CompletedTask;

	public virtual bool ShouldRender() => false;

	public virtual void OnAfterRender(bool firstRender) { }

	public abstract IChildRenderPolicy Child();

	public virtual ValueTask DisposeAsync()
		=> ValueTask.CompletedTask;

	public static IRenderPolicy Factory(RenderPolicy policy, IPlotRendering component)
		=> policy switch
		{
			RenderPolicy.Always => new AlwaysRenderPolicy(component),
			RenderPolicy.Never => new NeverRenderPolicy(component),
			RenderPolicy.Auto => new AutoRenderPolicy(component),
			_ => throw new GGNetInternalException("Not Implemented")
		};
}