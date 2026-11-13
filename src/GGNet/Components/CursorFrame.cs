namespace GGNet.Components;

internal sealed record CursorFrame(double X, double Top, double Bottom, string? Label, bool Pinned, bool Visible, IReadOnlyList<(double x, double y)> Markers);
