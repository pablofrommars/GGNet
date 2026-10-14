namespace GGNet.Coords;

internal sealed class GridComposition
{
	public List<GridLine> XLines { get; } = [];

	public List<GridRing> Rings { get; } = [];

	public List<GridRingPath> RingPaths { get; } = [];

	public List<GridLabel> XLabels { get; } = [];

	public List<GridLine> YLines { get; } = [];

	public List<GridLabel> YLabels { get; } = [];
}

internal enum GridClip
{
	Panel,
	Plot
}

internal readonly record struct GridLine(string Class, double X1, double X2, double Y1, double Y2);

internal readonly record struct GridRing(string Class, double CenterX, double CenterY, double Radius);

internal sealed record GridRingPath(string Class, IReadOnlyList<(double x, double y)> Points);

internal readonly record struct GridLabel(string Class, double X, double Y, string Anchor, Size FontSize, string Text, GridClip Clip);

internal readonly record struct GridInputs(
	bool XAxis,
	bool YAxis,
	IReadOnlyList<double> XBreaks,
	IReadOnlyList<double> XMinorBreaks,
	IReadOnlyList<(double f, string label)> XLabels,
	IReadOnlyList<(double f, string title)> XTitles,
	IReadOnlyList<double> YBreaks,
	IReadOnlyList<double> YMinorBreaks,
	IReadOnlyList<(double f, string label)> YLabels,
	double XLabelY,
	double XTitleY,
	double YLabelX);
