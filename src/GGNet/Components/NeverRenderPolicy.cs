namespace GGNet.Components;

public sealed class NeverRenderPolicy : RenderPolicyBase
{
	public NeverRenderPolicy(IPlot plot)
		: base(plot)
	{
	}

	public sealed class RenderChildPolicy : RenderChildPolicyBase
	{
	}

	public override RenderChildPolicyBase Child() => new RenderChildPolicy();
}
