using GGNet.Components;

namespace GGNet.Coords;

internal sealed class PolarCoordinateSystem(PolarOptions options, Style style) : ICoordinateSystem
{
	private readonly PolarOptions options = options;
	private readonly Style style = style;

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
}
