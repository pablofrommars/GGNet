using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms.Ribbon;

internal sealed class Ribbon<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	private readonly Dictionary<object, Shapes.Area> areas = [];

	public Ribbon(
		Source<T> source,
		Func<T, TX>? x,
		Func<T, TY>? ymin,
		Func<T, TY>? ymax,
		IAestheticMapping<T, string>? fill = null,
		Func<T, string>? tooltip = null,
		(bool x, bool y)? scale = null,
		bool inherit = true)
		: base(source, scale, inherit)
	{
		Selectors = new()
		{
			X = x,
			YMin = ymin,
			YMax = ymax,
			Tooltip = tooltip
		};

		Aesthetics = new()
		{
			Fill = fill
		};
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Aesthetics<T> Aesthetics { get; }

	public Positions<T> Positions { get; } = new();

	public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

	private Func<T, double, double, MouseEventArgs, Task>? onMouseOver;

	public Elements.Rectangle Aesthetic { get; set; } = default!;

	public override void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		if (Selectors.X is null)
		{
			Positions.X = XMapping(panel.Data.Selectors.X!, panel.X);
		}
		else
		{
			Positions.X = XMapping(Selectors.X, panel.X);
		}

		if (Selectors.YMin is null)
		{
			Positions.YMin = YMapping(panel.Data.Selectors.Y!, panel.Y);
		}
		else
		{
			Positions.YMin = YMapping(Selectors.YMin, panel.Y);
		}

		if (Selectors.YMax is null)
		{
			Positions.YMax = YMapping(panel.Data.Selectors.Y!, panel.Y);
		}
		else
		{
			Positions.YMax = YMapping(Selectors.YMax, panel.Y);
		}

		if (OnMouseOver is null && OnMouseOut is null && Selectors.Tooltip is not null)
		{
			onMouseOver = (item, x, y, _) =>
			{
				panel.Component?.Tooltip?.Show(
					x,
					y,
					0,
					Selectors.Tooltip(item),
					Aesthetics.Fill?.Map(item) ?? Aesthetic.Fill,
					Aesthetic.Alpha);

				return Task.CompletedTask;
			};

			OnMouseOut = (_, __) =>
			{
				panel.Component?.Tooltip?.Hide();

				return Task.CompletedTask;
			};
		}
		else if (OnMouseOver is not null)
		{
			onMouseOver = (item, _, __, e) => OnMouseOver(item, e);
		}

		if (!inherit)
		{
			return;
		}

		Aesthetics.Fill ??= panel.Data.Aesthetics.Fill as IAestheticMapping<T, string>;
	}

	public override void Train(T item)
	{
		Positions.X.Train(item);
		Positions.YMin.Train(item);
		Positions.YMax.Train(item);

		Aesthetics.Fill?.Train(item);
	}

	public override void Legend()
	{
		Legend(Aesthetics.Fill, value => new Elements.Rectangle
		{
			Fill = value,
			Alpha = Aesthetic.Alpha
		});
	}

	private Shapes.Area? _area = null;

	protected override void Shape(T item, bool flip)
	{
		Shapes.Area? area;

		if (Aesthetics.Fill is null)
		{
			if (_area is null)
			{
				_area = new Shapes.Area { Aesthetic = Aesthetic };

				Layer.Add(_area);
			}

			area = _area;
		}
		else
		{
			var fill = Aesthetics.Fill.Map(item);
			if (string.IsNullOrEmpty(fill))
			{
				return;
			}

			if (!areas.TryGetValue(fill, out area))
			{
				area = new Shapes.Area
				{
					Aesthetic = new()
					{
						Fill = fill,
						Alpha = Aesthetic.Alpha
					}
				};

				Layer.Add(area);

				areas[fill] = area;
			}
		}

		var x = Positions.X.Map(item);
		var ymin = Positions.YMin.Map(item);
		var ymax = Positions.YMax.Map(item);

		area.Points.Add((x, ymin, ymax));

		if (OnClick is not null || onMouseOver is not null || OnMouseOut is not null)
		{
			var aes = new Elements.Circle
			{
				Radius = 3.0,
				Alpha = 0,
				Fill = "transparent"
			};

			var circle1 = new Circle
			{
				X = x,
				Y = ymin,
				Aesthetic = aes
			};

			var circle2 = new Circle
			{
				X = x,
				Y = ymax,
				Aesthetic = aes
			};

			if (OnClick is not null)
			{
				circle1.OnClick = e => OnClick(item, e);
				circle2.OnClick = e => OnClick(item, e);
			}

			if (onMouseOver is not null)
			{
				circle1.OnMouseOver = e => onMouseOver(item, x, ymin, e);
				circle2.OnMouseOver = e => onMouseOver(item, x, ymax, e);
			}

			if (OnMouseOut is not null)
			{
				circle1.OnMouseOut = e => OnMouseOut(item, e);
				circle2.OnMouseOut = e => OnMouseOut(item, e);
			}

			Layer.Add(circle1);
			Layer.Add(circle2);
		}

		if (scale.x)
		{
			Positions.X.Position.Shape(x, x);
		}

		if (scale.y)
		{
			Positions.YMin.Position.Shape(ymin, ymax);
			Positions.YMax.Position.Shape(ymin, ymax);
		}
	}

	public override void Clear()
	{
		base.Clear();

		areas.Clear();
	}
}