namespace GGNet.Components;

public class NeverRenderPolicy : RenderPolicyBase
{
	public NeverRenderPolicy(IPlot plot)
		: base(plot)
	{
	}

	public class RenderChildPolicy : RenderChildPolicyBase
	{

	}

	public override RenderChildPolicyBase Child() => new RenderChildPolicy();
}
