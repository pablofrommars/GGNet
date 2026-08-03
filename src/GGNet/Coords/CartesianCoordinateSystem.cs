using GGNet.Components;

namespace GGNet.Coords;

internal sealed class CartesianCoordinateSystem(Style style) : ICoordinateSystem
{
	private readonly Style style = style;

	private Zone area;

	public bool CarvesAxisBands => true;

	public (double minMult, double minAdd, double maxMult, double maxAdd)? XExpansion(bool discrete) => null;

	public (double minMult, double minAdd, double maxMult, double maxAdd)? YExpansion(bool discrete) => null;

	public void Measure(Zone area, bool hasXTitles) => this.area = area;

	public (double x, double y) Project(double cx, double cy)
	  => (area.X + cx * area.Width, area.Y + (1.0 - cy) * area.Height);

	public (double cx, double cy) Unproject(double px, double py)
	  => ((px - area.X) / area.Width, 1.0 - (py - area.Y) / area.Height);

	public GridComposition ComposeGrid(GridInputs inputs)
	{
		var grid = new GridComposition();

		var y1 = area.Y;
		var y2 = area.Y + area.Height;

		foreach (var f in inputs.XBreaks)
		{
			var (x, _) = Project(f, 0.0);

			grid.XLines.Add(new("x-break", x, x, y1, y2));
		}

		foreach (var f in inputs.XMinorBreaks)
		{
			var (x, _) = Project(f, 0.0);

			grid.XLines.Add(new("x-minor-break", x, x, y1, y2));
		}

		if (inputs.XAxis)
		{
			foreach (var (f, label) in inputs.XLabels)
			{
				var (x, _) = Project(f, 0.0);

				if (area.X < x && x < (area.X + area.Width))
				{
					grid.XLabels.Add(new("x-break-label", x, inputs.XLabelY, style.Axis.Text.X.Anchor.Render(), style.Axis.Text.X.FontSize, label, GridClip.Plot));
				}
			}

			foreach (var (f, title) in inputs.XTitles)
			{
				var (x, _) = Project(f, 0.0);

				if (area.X < x && x < (area.X + area.Width))
				{
					grid.XLabels.Add(new("x-break-title", x, inputs.XTitleY, "middle", style.Axis.Title.X.FontSize, title, GridClip.Plot));
				}
			}
		}

		var x1 = area.X;
		var x2 = area.X + area.Width;

		foreach (var f in inputs.YBreaks)
		{
			var (_, y) = Project(0.0, f);

			grid.YLines.Add(new("y-break", x1, x2, y, y));
		}

		foreach (var f in inputs.YMinorBreaks)
		{
			var (_, y) = Project(0.0, f);

			grid.YLines.Add(new("y-minor-break", x1, x2, y, y));
		}

		if (inputs.YAxis)
		{
			var height = style.Axis.Text.Y.FontSize.Height();
			var offset = height / 4.0;

			foreach (var (f, label) in inputs.YLabels)
			{
				var y = Project(0.0, f).y + offset;

				if (area.Y < (y - height / 2.0) && (y + offset) < (area.Y + area.Height))
				{
					grid.YLabels.Add(new("y-break-label", inputs.YLabelX, y, style.Axis.Text.Y.Anchor.Render(), style.Axis.Text.Y.FontSize, label, GridClip.Panel));
				}
			}
		}

		return grid;
	}
}
