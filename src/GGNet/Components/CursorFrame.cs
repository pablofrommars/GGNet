namespace GGNet.Components;

internal sealed record CursorFrame(double X, double Top, double Bottom, string? Label, bool Pinned, bool Visible, IReadOnlyList<(double x, double y)> Markers, string? View = null)
{
	public string? InspectionStamp { get; init; }
	public double? Selection { get; init; }
	public double? CommittedSelection { get; init; }
	public bool Draw { get; init; } = true;
}
