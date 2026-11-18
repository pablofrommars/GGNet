namespace GGNet.Components;

internal sealed record CursorFrame(double X, double Top, double Bottom, string? Label, bool Pinned, bool Visible, IReadOnlyList<(double x, double y)> Markers, string? View = null)
{
	public string? InspectionStamp { get; init; }
	public double? Selection { get; init; }
	public double? CommittedSelection { get; init; }
	public bool Draw { get; init; } = true;
	public double? LabelX { get; init; }
	public double? LabelTop { get; init; }
	public double? LabelWidth { get; init; }
	public double? LabelFontSize { get; init; }
	public double? MeasureFontSize { get; init; }
	public string? MeasureKey { get; init; }
	public bool LabelReady { get; init; } = true;
	public bool ShowLabel { get; init; } = true;
}
