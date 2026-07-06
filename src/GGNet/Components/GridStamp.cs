using GGNet.Scales;

namespace GGNet.Components;

// Exact snapshot of every input the panel's layout and grid composition read.
// Interactive refreshes recompose only when the stamp changes; equality is
// structural, so a hit is a proof of identical output, not a heuristic.
// (Strips, axis flags, labs and Style are fixed per panel registration and
// need no stamping; re-registration resets the stamp.)
internal sealed class GridStamp
{
	private Zone outer;
	private (double min, double max) xRange;
	private (double min, double max) yRange;
	private (double width, double height) axis;
	private double[] xBreaks = [];
	private double[] xMinorBreaks = [];
	private double[] yBreaks = [];
	private double[] yMinorBreaks = [];
	private (double value, string label)[] xLabels = [];
	private (double value, string title)[] xTitles = [];
	private (double value, string label)[] yLabels = [];

	public static GridStamp Capture<TX, TY>(Zone outer, Position<TX> xscale, Position<TY> yscale, (double width, double height) axis)
		where TX : struct
		where TY : struct
		=> new()
		{
			outer = outer,
			xRange = xscale.Range,
			yRange = yscale.Range,
			axis = axis,
			xBreaks = [.. xscale.Breaks],
			xMinorBreaks = [.. xscale.MinorBreaks],
			yBreaks = [.. yscale.Breaks],
			yMinorBreaks = [.. yscale.MinorBreaks],
			xLabels = [.. xscale.Labels],
			xTitles = [.. xscale.Titles],
			yLabels = [.. yscale.Labels]
		};

	public bool Matches(GridStamp? other)
	{
		if (other is null)
		{
			return false;
		}

		return outer.Equals(other.outer)
			&& xRange == other.xRange
			&& yRange == other.yRange
			&& axis == other.axis
			&& xBreaks.AsSpan().SequenceEqual(other.xBreaks)
			&& xMinorBreaks.AsSpan().SequenceEqual(other.xMinorBreaks)
			&& yBreaks.AsSpan().SequenceEqual(other.yBreaks)
			&& yMinorBreaks.AsSpan().SequenceEqual(other.yMinorBreaks)
			&& xLabels.AsSpan().SequenceEqual(other.xLabels)
			&& xTitles.AsSpan().SequenceEqual(other.xTitles)
			&& yLabels.AsSpan().SequenceEqual(other.yLabels);
	}
}
