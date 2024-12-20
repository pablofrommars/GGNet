using GGNet.Exceptions;

namespace GGNet.Rendering;

public abstract class RenderPolicyBase(IPlotRendering plot) : IRenderPolicy
{
	protected readonly IPlotRendering plot = plot;

    public virtual Task RefreshAsync(RenderTarget target, CancellationToken token)
		=> Task.CompletedTask;

	public virtual bool ShouldRender() => false;

	public virtual void OnAfterRender(bool firstRender) { }

	public abstract IChildRenderPolicy Child();

	public virtual ValueTask DisposeAsync()
		=> ValueTask.CompletedTask;

	public static IRenderPolicy Factory(RenderPolicy policy, IPlotRendering component)
		=> policy switch
		{
			RenderPolicy.Active => new ActiveRenderPolicy(component),
			RenderPolicy.Always => new AlwaysRenderPolicy(component),
			RenderPolicy.Never => new NeverRenderPolicy(component),
			_ => throw new GGNetInternalException("Not Implemented")
		};
}
