using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;
namespace GGNet.Geoms.Map;

internal sealed class Map<T> : Geom<T, double, double>
{
	private readonly bool animation;

	public Map(
		Source<T> source,
		Func<T, Geospacial.Polygon[]> polygons,
		IAestheticMapping<T, string>? fill = null,
		Func<T, (Geospacial.Point point, string content)>? tooltip = null,
		bool animation = false,
		(bool x, bool y)? scale = null,
		bool inherit = true)
		: base(source, scale, inherit)
	{
		Selectors = new()
		{
			Polygons = polygons,
			Tooltip = tooltip
		};

		Aesthetics = new()
		{
			Fill = fill
		};

		this.animation = animation;
	}

	public Selectors<T> Selectors { get; }

	public Aesthetics<T> Aesthetics { get; }

	public Positions<T> Positions { get; } = new();

	public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

	public Elements.Rectangle Aesthetic { get; set; } = default!;

	public override void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		Positions.X = XMapping(panel.Data.Selectors.X!, panel.X);
		Positions.Y = YMapping(panel.Data.Selectors.Y!, panel.Y);

		if (OnMouseOver is null && OnMouseOut is null && Selectors.Tooltip is not null)
		{
			OnMouseOver = (item, _) =>
			{
				var (point, content) = Selectors.Tooltip(item);

				if (point is not null)
				{
					panel.Component?.Tooltip?.Show(
						point.Longitude,
						point.Latitude,
						0,
						content,
						Aesthetics.Fill?.Map(item) ?? Aesthetic.Fill,
						Aesthetic.Alpha);
				}

				return Task.CompletedTask;
			};

			OnMouseOut = (_, __) =>
			{
				panel.Component?.Tooltip?.Hide();

				return Task.CompletedTask;
			};
		}

		if (!inherit)
		{
			return;
		}

		Aesthetics.Fill ??= panel.Data.Aesthetics.Fill as IAestheticMapping<T, string>;
	}

	public override void Train(T item)
	{
		Aesthetics.Fill?.Train(item);
	}

	public override void Legend()
	{
		Legend(Aesthetics.Fill, value => new Elements.Rectangle
		{
			Fill = value,
			Alpha = Aesthetic.Alpha,
			Color = Aesthetic.Color,
			Width = Aesthetic.Width
		});
	}

	protected override void Shape(T item, bool flip)
	{
		var fill = Aesthetic.Fill;

		if (Aesthetics.Fill is not null)
		{
			fill = Aesthetics.Fill.Map(item);
			if (string.IsNullOrEmpty(fill))
			{
				return;
			}
		}

		var polygons = Selectors.Polygons(item);

		var xmin = double.MaxValue;
		var xmax = double.MinValue;

		var ymin = double.MaxValue;
		var ymax = double.MinValue;

		for (var i = 0; i < polygons.Length; i++)
		{
			var polygon = polygons[i];
			for (var j = 0; j < polygon.Longitude.Length; j++)
			{
				var x = polygon.Longitude[j];
				var y = polygon.Latitude[j];

				if (x < xmin)
				{
					xmin = x;
				}

				if (x > xmax)
				{
					xmax = x;
				}

				if (y < ymin)
				{
					ymin = y;
				}

				if (y > ymax)
				{
					ymax = y;
				}
			}
		}

		var multi = new MultiPolygon
		{
			Classes = animation ? "animate-map" : string.Empty,
			Polygons = polygons,
			Aesthetic = new()
			{
				Fill = fill,
				Alpha = Aesthetic.Alpha,
				Color = Aesthetic.Color,
				Width = Aesthetic.Width
			}
		};

		if (OnClick is not null)
		{
			multi.OnClick = e => OnClick(item, e);
		}

		if (OnMouseOver is not null)
		{
			multi.OnMouseOver = e => OnMouseOver(item, e);
		}

		if (OnMouseOut is not null)
		{
			multi.OnMouseOut = e => OnMouseOut(item, e);
		}

		Layer.Add(multi);

		if (scale.x)
		{
			Positions.X.Position.Shape(xmin, xmax);
		}

		if (scale.y)
		{
			Positions.Y.Position.Shape(ymin, ymax);
		}
	}
}