using GGNet.Components;
using GGNet.Exceptions;

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

	public void Measure(Zone area, bool hasXTitles)
	{
		// Polar draws breaks and labels inside the plotting area: inscribe the
		// circle, minus a gutter for the angular labels around it — one line, or
		// two when the scale carries break titles.
		var gutter = style.Axis.Text.X.FontSize.Height() + style.Polar.LabelMargin;

		if (hasXTitles)
		{
			gutter += TitleOffset();
		}

		CenterX = area.X + area.Width / 2.0;
		CenterY = area.Y + area.Height / 2.0;
		Radius = Math.Max(0.0, Math.Min(area.Width, area.Height) / 2.0 - gutter);
	}

	// Baseline distance from a break label to its title line: the title's own
	// line height plus clearance for the label's descenders.
	private double TitleOffset()
	  => style.Axis.Title.X.FontSize.Height() + 0.25 * style.Axis.Text.X.FontSize.Height();

	public (double x, double y) Project(double cx, double cy)
	  => Polar.Project(cx, cy, CenterX, CenterY, Radius, options.StartAngle, options.Clockwise);

	public (double cx, double cy) Unproject(double px, double py)
	  => throw new GGNetUserException("Unproject is not supported with polar coordinates");

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

		// With a title line the two-line stack shifts outward along the spoke:
		// fully up at the top spoke (the title takes the single line's place),
		// not at all at the bottom (the title flows downward), half at the
		// sides — so every line stays outside the web.
		var titleOffset = inputs.XTitles.Count > 0 ? TitleOffset() : 0.0;

		(double x, double y, string anchor) Anchor(double f)
		{
			var theta = Polar.Angle(f, options.StartAngle, options.Clockwise);

			var cos = Math.Cos(theta);
			var sin = Math.Sin(theta);

			var x = CenterX + labelRadius * cos;
			var y = CenterY + labelRadius * sin + (sin + 1.0) / 2.0 * fontHeight * 0.8 - (1.0 - (sin + 1.0) / 2.0) * titleOffset;

			var anchor = cos < -0.1 ? "end" : cos > 0.1 ? "start" : "middle";

			return (x, y, anchor);
		}

		foreach (var (f, label) in inputs.XLabels)
		{
			var (x, y, anchor) = Anchor(f);

			grid.XLabels.Add(new("x-break-label", x, y, anchor, style.Axis.Text.X.FontSize, label, GridClip.Panel));
		}

		foreach (var (f, title) in inputs.XTitles)
		{
			var (x, y, anchor) = Anchor(f);

			grid.XLabels.Add(new("x-break-title", x, y + titleOffset, anchor, style.Axis.Title.X.FontSize, title, GridClip.Panel));
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
