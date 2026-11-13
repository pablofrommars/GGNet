namespace GGNet.Components;

internal interface IPanel
{
	void Refresh() { }

	void RefreshCursor() { }

	ITooltip? Tooltip { get; }
}
