namespace GGNet.Components;

public interface IPanel
{
	void Refresh(RenderTarget target) { }

	ITooltip? Tooltip { get; }
}
