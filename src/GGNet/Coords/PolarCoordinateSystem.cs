using GGNet.Components;

namespace GGNet.Coords;

internal sealed class PolarCoordinateSystem(PolarOptions options, Style style) : ICoordinateSystem
{
	private readonly PolarOptions options = options;
	private readonly Style style = style;

	public bool CarvesAxisBands => false;

	// The angular scale spans the full turn: discrete (0,0,0,1) maps n
	// categories to fractions i/n with the extra slot as the closing wrap
	// segment. The radial scale is zero-based at the center.
	public (double minMult, double minAdd, double maxMult, double maxAdd)? XExpansion(bool discrete)
	  => discrete ? (0.0, 0.0, 0.0, 1.0) : (0.0, 0.0, 0.0, 0.0);

	public (double minMult, double minAdd, double maxMult, double maxAdd)? YExpansion(bool discrete)
	  => discrete ? null : (0.0, 0.0, 0.05, 0.0);

	public double CenterX { get; private set; }

	public double CenterY { get; private set; }

	public double Radius { get; private set; }

	public void Measure(Zone area)
	{
		// Polar draws breaks and labels inside the plotting area: inscribe the
		// circle, minus a gutter for the angular labels around it.
		var gutter = style.Axis.Text.X.FontSize.Height() + style.Polar.LabelMargin;

		CenterX = area.X + area.Width / 2.0;
		CenterY = area.Y + area.Height / 2.0;
		Radius = Math.Max(0.0, Math.Min(area.Width, area.Height) / 2.0 - gutter);
	}

	public (double x, double y) Project(double cx, double cy)
	  => Polar.Project(cx, cy, CenterX, CenterY, Radius, options.StartAngle, options.Clockwise);

	public GridComposition ComposeGrid(GridInputs inputs)
	{
		var grid = new GridComposition();

		var spokes = inputs.XBreaks;
		var web = style.Polar.Rings == PolarRingType.Polygon && spokes.Count >= 3;

		foreach (var f in spokes)
		{
			var (px, py) = Project(f, 1.0);

			grid.XLines.Add(new("x-break", CenterX, px, CenterY, py));
		}

		ComposeRings(grid, inputs.YBreaks, "y-break", web, spokes);
		ComposeRings(grid, inputs.YMinorBreaks, "y-minor-break", web, spokes);

		var fontHeight = style.Axis.Text.X.FontSize.Height();
		var labelRadius = Radius + style.Polar.LabelMargin;

		foreach (var (f, label) in inputs.XLabels)
		{
			var theta = Polar.Angle(f, options.StartAngle, options.Clockwise);

			var cos = Math.Cos(theta);
			var sin = Math.Sin(theta);

			var x = CenterX + labelRadius * cos;
			var y = CenterY + labelRadius * sin + (sin + 1.0) / 2.0 * fontHeight * 0.8;

			var anchor = cos < -0.1 ? "end" : cos > 0.1 ? "start" : "middle";

			grid.XLabels.Add(new("x-break-label", x, y, anchor, style.Axis.Text.X.FontSize, label, GridClip.Panel));
		}

		foreach (var (f, label) in inputs.YLabels)
		{
			if (f < 0.0 || f > 1.0)
			{
				continue;
			}

			var (px, py) = Project(0.0, f);

			grid.YLabels.Add(new("y-break-label", px + 4, py - 2, "start", style.Axis.Text.Y.FontSize, label, GridClip.Panel));
		}

		return grid;
	}

	private void ComposeRings(GridComposition grid, IReadOnlyList<double> fractions, string @class, bool web, IReadOnlyList<double> spokes)
	{
		foreach (var f in fractions)
		{
			if (f < 0.0 || f > 1.0)
			{
				continue;
			}

			if (web)
			{
				var points = new List<(double x, double y)>(spokes.Count);

				foreach (var spoke in spokes)
				{
					points.Add(Project(spoke, f));
				}

				grid.RingPaths.Add(new(@class, points));
			}
			else
			{
				grid.Rings.Add(new(@class, CenterX, CenterY, f * Radius));
			}
		}
	}
}
