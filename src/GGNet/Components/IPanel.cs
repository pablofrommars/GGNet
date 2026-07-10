namespace GGNet.Components;

internal interface IPanel
{
	void Refresh() { }

	ITooltip? Tooltip { get; }
}
