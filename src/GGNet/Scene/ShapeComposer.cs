using GGNet.Components;
using GGNet.Geoms;
using GGNet.Shapes;

namespace GGNet.Scene;

// The compose stage for shapes: walks the geoms' data-space layers and
// produces screen-space primitives. All projection and annotation-label
// geometry lives here; the Area component is a dumb walker over the result.
internal static class ShapeComposer
{
	private static readonly ObjectPool<StringBuilder> pool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

	public static List<IScreenPrimitive> Compose(IReadOnlyList<IGeom> geoms, ICoord coord, Zone zone)
	{
		var scene = new List<IScreenPrimitive>();

		for (var g = 0; g < geoms.Count; g++)
		{
			var geom = geoms[g];

			for (var l = 0; l < geom.Layer.Count; l++)
			{
				var shape = geom.Layer[l];

				switch (shape)
				{
					case Circle circle:
						ComposeCircle(scene, coord, shape, circle);
						break;

					case Line line:
						ComposeLine(scene, coord, shape, line);
						break;

					case Rectangle rectangle:
						ComposeRectangle(scene, coord, shape, rectangle);
						break;

					case Shapes.Area area:
						ComposeArea(scene, coord, area);
						break;

					case Shapes.Path path:
						ComposePath(scene, coord, path);
						break;

					case Polygon polygon:
						ComposePolygon(scene, coord, shape, polygon);
						break;

					case MultiPolygon multi:
						ComposeMultiPolygon(scene, coord, shape, multi);
						break;

					case Text text:
						ComposeText(scene, coord, text);
						break;

					case VLine vline:
						ComposeVLine(scene, coord, zone, vline);
						break;

					case HLine hline:
						ComposeHLine(scene, coord, zone, hline);
						break;

					case ABLine abline:
						ComposeABLine(scene, coord, zone, abline);
						break;
				}
			}
		}

		return scene;
	}

	private static void ComposeCircle(List<IScreenPrimitive> scene, ICoord coord, IShape shape, Circle circle)
	{
		var (px, py) = coord.Project(circle.X, circle.Y);

		scene.Add(new ScreenCircle(
			shape.Css,
			px,
			py,
			circle.Aesthetic.Radius,
			circle.Aesthetic.Fill,
			circle.Aesthetic.StrokeOpacity,
			circle.Aesthetic.FillOpacity,
			shape.OnClickHandler,
			shape.OnMouseOverHandler,
			shape.OnMouseOutHandler));
	}

	private static void ComposeLine(List<IScreenPrimitive> scene, ICoord coord, IShape shape, Line line)
	{
		var (px1, py1) = coord.Project(line.X1, line.Y1);
		var (px2, py2) = coord.Project(line.X2, line.Y2);

		scene.Add(new ScreenLine(
			shape.Css,
			px1,
			py1,
			px2,
			py2,
			line.Aesthetic.StrokeWidth,
			line.Aesthetic.Stroke,
			line.Aesthetic.StrokeOpacity,
			line.Aesthetic.LineType,
			shape.OnClickHandler,
			shape.OnMouseOverHandler,
			shape.OnMouseOutHandler));
	}

	private static void ComposeRectangle(List<IScreenPrimitive> scene, ICoord coord, IShape shape, Rectangle rectangle)
	{
		var x = coord.ToX(rectangle.X);
		var y = coord.ToY(rectangle.Y + rectangle.Height);
		var width = coord.ToX(rectangle.X + rectangle.Width) - x;
		var height = coord.ToY(rectangle.Y) - y;

		scene.Add(new ScreenRect(
			shape.Css,
			x,
			y,
			width,
			height,
			rectangle.Aesthetic.Fill,
			rectangle.Aesthetic.FillOpacity,
			rectangle.Aesthetic.Stroke,
			rectangle.Aesthetic.StrokeOpacity,
			rectangle.Aesthetic.StrokeWidth,
			shape.OnClickHandler,
			shape.OnMouseOverHandler,
			shape.OnMouseOutHandler));
	}

	private static void ComposeArea(List<IScreenPrimitive> scene, ICoord coord, Shapes.Area area)
	{
		if (area.Points.Count == 0)
		{
			return;
		}

		var sb = pool.Get();
		try
		{
			var (x, _, ymax) = area.Points[0];

			var (px, py) = coord.Project(x, ymax);

			sb.Append(CultureInfo.InvariantCulture, $"M {px} {py}");

			for (var j = 1; j < area.Points.Count; j++)
			{
				(x, _, ymax) = area.Points[j];

				(px, py) = coord.Project(x, ymax);

				sb.Append(CultureInfo.InvariantCulture, $" L {px} {py}");
			}

			for (var j = 0; j < area.Points.Count; j++)
			{
				double ymin;
				(x, ymin, _) = area.Points[area.Points.Count - j - 1];

				(px, py) = coord.Project(x, ymin);

				sb.Append(CultureInfo.InvariantCulture, $" L {px} {py}");
			}

			sb.Append(" Z");

			scene.Add(new ScreenFill(sb.ToString(), area.Aesthetic.Fill, area.Aesthetic.FillOpacity));
		}
		finally
		{
			sb.Clear();
			pool.Return(sb);
		}
	}

	private static void ComposePath(List<IScreenPrimitive> scene, ICoord coord, Shapes.Path path)
	{
		if (path.Points.Count == 0)
		{
			return;
		}

		var sb = pool.Get();
		try
		{
			var M = true;

			for (var j = 0; j < path.Points.Count; j++)
			{
				var (x, y) = path.Points[j];

				if (double.IsNaN(y))
				{
					M = true;
				}
				else
				{
					var (px, py) = coord.Project(x, y);

					sb.Append(CultureInfo.InvariantCulture, $"{(M ? " M " : " L ")}{px} {py}");

					M = false;
				}
			}

			scene.Add(new ScreenStroke(
				sb.ToString(),
				path.Aesthetic.StrokeWidth,
				path.Aesthetic.Stroke,
				path.Aesthetic.StrokeOpacity,
				path.Aesthetic.LineType));
		}
		finally
		{
			sb.Clear();
			pool.Return(sb);
		}
	}

	private static void AppendPolygon(StringBuilder sb, ICoord coord, Geospacial.Polygon poly)
	{
		var (px, py) = coord.Project(poly.Longitude[0], poly.Latitude[0]);

		sb.Append(CultureInfo.InvariantCulture, $"M {px} {py}");

		for (var i = 1; i < poly.Longitude.Length; i++)
		{
			(px, py) = coord.Project(poly.Longitude[i], poly.Latitude[i]);

			sb.Append(CultureInfo.InvariantCulture, $" L {px} {py}");
		}

		sb.Append(" Z");
	}

	private static void ComposePolygon(List<IScreenPrimitive> scene, ICoord coord, IShape shape, Polygon polygon)
	{
		var sb = pool.Get();
		try
		{
			AppendPolygon(sb, coord, polygon.Path);

			scene.Add(new ScreenPolygon(
				shape.Css,
				sb.ToString(),
				polygon.Aesthetic.Fill,
				polygon.Aesthetic.FillOpacity,
				polygon.Aesthetic.Stroke,
				polygon.Aesthetic.StrokeWidth,
				shape.OnClickHandler,
				shape.OnMouseOverHandler,
				shape.OnMouseOutHandler));
		}
		finally
		{
			sb.Clear();
			pool.Return(sb);
		}
	}

	private static void ComposeMultiPolygon(List<IScreenPrimitive> scene, ICoord coord, IShape shape, MultiPolygon multi)
	{
		var sb = pool.Get();
		try
		{
			AppendPolygon(sb, coord, multi.Polygons[0]);

			for (var i = 1; i < multi.Polygons.Length; i++)
			{
				sb.Append(' ');

				AppendPolygon(sb, coord, multi.Polygons[i]);
			}

			scene.Add(new ScreenPolygon(
				shape.Css,
				sb.ToString(),
				multi.Aesthetic.Fill,
				multi.Aesthetic.FillOpacity,
				multi.Aesthetic.Stroke,
				multi.Aesthetic.StrokeWidth,
				shape.OnClickHandler,
				shape.OnMouseOverHandler,
				shape.OnMouseOutHandler));
		}
		finally
		{
			sb.Clear();
			pool.Return(sb);
		}
	}

	private static void ComposeText(List<IScreenPrimitive> scene, ICoord coord, Text text)
	{
		var (px, py) = coord.Project(text.X, text.Y);

		scene.Add(new ScreenText(
			string.Create(CultureInfo.InvariantCulture, $"translate({px}, {py}) rotate({text.Aesthetic.Angle}deg)"),
			text.Aesthetic.Color,
			text.Aesthetic.Anchor.Render(),
			text.Aesthetic.FontSize,
			text.Aesthetic.FontWeight,
			text.Aesthetic.FontStyle,
			text.Value));
	}

	private static void ComposeVLine(List<IScreenPrimitive> scene, ICoord coord, Zone zone, VLine vline)
	{
		var x = coord.ToX(vline.X);

		scene.Add(new ScreenRule(
			x,
			zone.Y,
			x,
			zone.Y + zone.Height,
			vline.Line.StrokeWidth,
			vline.Line.Stroke,
			vline.Line.StrokeOpacity,
			vline.Line.LineType));

		if (string.IsNullOrEmpty(vline.Label))
		{
			return;
		}

		var offset = 0.025 * zone.Height;

		var y = 0.0;
		var angle = 0.0;

		if (vline.Text.Anchor == Anchor.End)
		{
			x += 3;
			y = zone.Y + offset;
			angle = 90;
		}
		else
		{
			x -= 3;
			y = zone.Y + zone.Height - offset;
			angle = -90;
		}

		scene.Add(new ScreenLabel(
			vline.Text.Color,
			vline.Text.Opacity,
			Anchor.Start.Render(),
			vline.Text.FontSize,
			vline.Text.FontWeight,
			vline.Text.FontStyle,
			string.Create(CultureInfo.InvariantCulture, $"translate({x}px, {y}px) rotate({angle}deg)"),
			vline.Label));
	}

	private static void ComposeHLine(List<IScreenPrimitive> scene, ICoord coord, Zone zone, HLine hline)
	{
		var y = coord.ToY(hline.Y);

		scene.Add(new ScreenRule(
			zone.X,
			y,
			zone.X + zone.Width,
			y,
			hline.Line.StrokeWidth,
			hline.Line.Stroke,
			hline.Line.StrokeOpacity,
			hline.Line.LineType));

		if (string.IsNullOrEmpty(hline.Label))
		{
			return;
		}

		var offset = 0.025 * zone.Width;

		var x = 0.0;

		if (hline.Text.Anchor == Anchor.End)
		{
			x = zone.X + zone.Width - offset;
		}
		else
		{
			x = zone.X + offset;
		}

		y -= 3;

		scene.Add(new ScreenLabel(
			hline.Text.Color,
			hline.Text.Opacity,
			hline.Text.Anchor.Render(),
			hline.Text.FontSize,
			hline.Text.FontWeight,
			hline.Text.FontStyle,
			string.Create(CultureInfo.InvariantCulture, $"translate({x}, {y})"),
			hline.Label));
	}

	private static void ComposeABLine(List<IScreenPrimitive> scene, ICoord coord, Zone zone, ABLine abline)
	{
		var ymin = coord.XRange.min;
		var ymax = coord.XRange.max;
		if (abline.Transformation.x)
		{
			ymin = coord.XTransformation.Inverse(ymin);
			ymax = coord.XTransformation.Inverse(ymax);
		}

		ymin = abline.A * ymin + abline.B;
		ymax = abline.A * ymax + abline.B;

		if (abline.Transformation.y)
		{
			ymin = coord.YTransformation.Apply(ymin);
			ymax = coord.YTransformation.Apply(ymax);
		}

		var y1 = coord.ToY(ymin);
		var y2 = coord.ToY(ymax);

		scene.Add(new ScreenRule(
			zone.X,
			y1,
			zone.X + zone.Width,
			y2,
			abline.Line.StrokeWidth,
			abline.Line.Stroke,
			abline.Line.StrokeOpacity,
			abline.Line.LineType));

		if (string.IsNullOrEmpty(abline.Label))
		{
			return;
		}

		var x = 0.0;
		var y = 0.0;
		var offset = 0.05 * zone.Width;

		var tan = (y1 - y2) / zone.Width;
		var angle = Math.Atan(tan);

		if (abline.Text.Anchor == Anchor.End)
		{
			x = zone.X + zone.Width;
			y = y2;

			if (angle >= 0)
			{
				if (ymax > coord.YRange.max)
				{
					x = coord.YRange.max;

					if (abline.Transformation.y)
					{
						x = coord.YTransformation.Inverse(x);
					}

					x = (x - abline.B) / abline.A;

					if (abline.Transformation.x)
					{
						x = coord.XTransformation.Apply(x);
					}

					x = coord.ToX(x);

					y = zone.Y;
				}
			}
			else
			{
				if (ymax < coord.YRange.min)
				{
					x = coord.YRange.min;

					if (abline.Transformation.y)
					{
						x = coord.YTransformation.Inverse(x);
					}

					x = (x - abline.B) / abline.A;

					if (abline.Transformation.x)
					{
						x = coord.XTransformation.Apply(x);
					}

					x = coord.ToX(x);

					y = zone.Y + zone.Height;
				}
			}

			x -= offset;
			y += offset * tan - 3;
		}
		else
		{
			x = zone.X;
			y = y1;

			if (angle >= 0)
			{
				if (ymin < coord.YRange.min)
				{
					x = coord.YRange.min;

					if (abline.Transformation.y)
					{
						x = coord.YTransformation.Inverse(x);
					}

					x = (x - abline.B) / abline.A;

					if (abline.Transformation.x)
					{
						x = coord.XTransformation.Apply(x);
					}

					x = coord.ToX(x);

					y = zone.Y + zone.Height;
				}
			}
			else
			{
				if (ymin < coord.YRange.max)
				{
					x = coord.YRange.max;

					if (abline.Transformation.y)
					{
						x = coord.YTransformation.Inverse(x);
					}

					x = (x - abline.B) / abline.A;

					if (abline.Transformation.x)
					{
						x = coord.XTransformation.Apply(x);
					}

					x = coord.ToX(x);

					y = zone.Y;
				}
			}

			x += offset;
			y -= offset * tan + 3;
		}

		scene.Add(new ScreenAngledLabel(
			string.Create(CultureInfo.InvariantCulture, $"translate({x}, {y}) rotate({-angle}rad)"),
			abline.Text.Color,
			abline.Text.Opacity,
			abline.Text.Anchor.Render(),
			abline.Text.FontSize,
			abline.Text.FontWeight,
			abline.Text.FontStyle,
			abline.Label));
	}
}
