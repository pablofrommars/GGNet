namespace GGNet.Components;

internal interface IPanel
{
	void Refresh(RenderTarget target) { }

	ITooltip? Tooltip { get; }
}
