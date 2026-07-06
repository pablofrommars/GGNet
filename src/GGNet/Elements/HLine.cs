namespace GGNet.Elements;

internal readonly record struct HLine
{
	public required string Stroke { get; init; }

	public double StrokeOpacity { get; init; }

	public double StrokeWidth { get; init; }

	public LineType LineType { get; init; }
}
