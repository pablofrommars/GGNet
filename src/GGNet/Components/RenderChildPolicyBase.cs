namespace GGNet.Components;

public abstract class RenderChildPolicyBase
{
	public virtual void Refresh() { }

	public virtual bool ShouldRender() => false;
}