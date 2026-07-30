using static System.Math;

namespace GGNet.Shapes;

// Visible-y measurement for the auto-fit: the y extent each drawn shape
// contributes inside an x window, in mapped double space (the space geoms
// shape in and ViewRange lives in). Measuring what's drawn — not the raw
// data — makes the fit exact for bar baselines, ribbon bands and layered
// sources, while statistics stay frozen: only their output is measured.
internal static class ShapeExtents
{
	public static (double min, double max) VisibleY(IReadOnlyList<Shape> layer, (double min, double max) window)
	{
		var y = (min: double.PositiveInfinity, max: double.NegativeInfinity);

		for (var i = 0; i < layer.Count; i++)
		{
			// Exhaustive over the Shape union (CS8509): a new variant is a
			// compile error until it declares its visible-y contribution.
			y = layer[i] switch
			{
				Circle circle => Point(y, window, circle.X, circle.Y),
				Text text => Point(y, window, text.X, text.Y),
				Line line => Segment(y, window, line.X1, line.Y1, line.X2, line.Y2),
				Rectangle rectangle => Span(y, window, rectangle.X, rectangle.X + rectangle.Width, rectangle.Y, rectangle.Y + rectangle.Height),
				// A bounded reference line participates (it trains scales too).
				HLine hline => Include(y, hline.Y),
				VLine => y,
				// An unbounded diagonal annotation would fight the fit; skipped.
				ABLine => y,
				Area area => AreaPoints(y, window, area),
				Path path => PathPoints(y, window, path),
				Polygon polygon => Ring(y, window, polygon.Path),
				MultiPolygon multi => Rings(y, window, multi),
			};
		}

		return y;
	}

	private static (double min, double max) Include((double min, double max) y, double value)
		=> double.IsNaN(value) ? y : (Min(y.min, value), Max(y.max, value));

	private static (double min, double max) Point((double min, double max) y, (double min, double max) window, double x, double value)
		=> x < window.min || x > window.max ? y : Include(y, value);

	private static (double min, double max) Span((double min, double max) y, (double min, double max) window, double x1, double x2, double value1, double value2)
		=> x2 < window.min || x1 > window.max ? y : Include(Include(y, value1), value2);

	// A segment contributes its endpoints clamped to the window, interpolating
	// at the boundary — a segment fully spanning the window still counts.
	private static (double min, double max) Segment((double min, double max) y, (double min, double max) window, double x1, double y1, double x2, double y2)
	{
		if (x2 < x1)
		{
			(x1, x2) = (x2, x1);
			(y1, y2) = (y2, y1);
		}

		if (x2 < window.min || x1 > window.max)
		{
			return y;
		}

		if (x1 < window.min && x2 > x1)
		{
			y1 += (y2 - y1) * (window.min - x1) / (x2 - x1);
			x1 = window.min;
		}

		if (x2 > window.max && x2 > x1)
		{
			y2 = y1 + (y2 - y1) * (window.max - x1) / (x2 - x1);
		}

		return Include(Include(y, y1), y2);
	}

	private static (double min, double max) PathPoints((double min, double max) y, (double min, double max) window, Path path)
	{
		var points = path.Points;

		if (points.Count == 1)
		{
			return Point(y, window, points[0].x, points[0].y);
		}

		for (var i = 1; i < points.Count; i++)
		{
			y = Segment(y, window, points[i - 1].x, points[i - 1].y, points[i].x, points[i].y);
		}

		return y;
	}

	private static (double min, double max) AreaPoints((double min, double max) y, (double min, double max) window, Area area)
	{
		var points = area.Points;

		if (points.Count == 1)
		{
			y = Point(y, window, points[0].x, points[0].ymin);

			return Point(y, window, points[0].x, points[0].ymax);
		}

		for (var i = 1; i < points.Count; i++)
		{
			y = Segment(y, window, points[i - 1].x, points[i - 1].ymin, points[i].x, points[i].ymin);
			y = Segment(y, window, points[i - 1].x, points[i - 1].ymax, points[i].x, points[i].ymax);
		}

		return y;
	}

	// Polygon vertices inside the window; edge interpolation is deliberately
	// skipped — maps pan by roam, not by axis window.
	private static (double min, double max) Ring((double min, double max) y, (double min, double max) window, Geospacial.Polygon path)
	{
		for (var i = 0; i < path.Longitude.Length; i++)
		{
			y = Point(y, window, path.Longitude[i], path.Latitude[i]);
		}

		return y;
	}

	private static (double min, double max) Rings((double min, double max) y, (double min, double max) window, MultiPolygon multi)
	{
		for (var i = 0; i < multi.Polygons.Length; i++)
		{
			y = Ring(y, window, multi.Polygons[i]);
		}

		return y;
	}
}
