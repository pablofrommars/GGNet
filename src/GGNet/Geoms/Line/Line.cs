using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;
using GGNet.Exceptions;

namespace GGNet.Geoms.Line;

internal sealed partial class Line<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
	private readonly Dictionary<(string, LineType), Shapes.Path> paths = [];

	private readonly bool piecewise;

	public Line(
	  IReadOnlyList<T> source,
	  Func<T, TX>? x,
	  Func<T, TY>? y,
	  IAestheticMapping<T, string>? color = null,
	  IAestheticMapping<T, LineType>? lineType = null,
	  Func<T, RenderFragment>? tooltip = null,
	  (bool x, bool y)? scale = null,
	  bool piecewise = false)
	  : base(source, scale)
	{
		this.piecewise = piecewise;

		Selectors = new()
		{
			X = x,
			Y = y,
			Tooltip = tooltip
		};

		Aesthetics = new()
		{
			Color = color,
			LineType = lineType
		};
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Aesthetics<T> Aesthetics { get; }

	public Positions<T> Positions { get; } = new();

	public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

	private Func<T, double, double, MouseEventArgs, Task>? onMouseOver;

	public required Elements.Line Aesthetic { get; set; }

	public override void Init<T1>(Panel<T1, TX, TY> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		if (Selectors.X is null)
		{
			throw new GGNetUserException("X selector is required");
		}

		Positions.X = new PositionMapping<T, TX>(Selectors.X, panel.X);

		if (Selectors.Y is null)
		{
			throw new GGNetUserException("Y selector is required");
		}

		Positions.Y = new PositionMapping<T, TY>(Selectors.Y, panel.Y);

		if (OnMouseOver is null && OnMouseOut is null && Selectors.Tooltip is not null)
		{
			onMouseOver = (item, x, y, _) =>
			{
				panel.Component?.Tooltip?.Show(
			x,
			y,
			0,
			Selectors.Tooltip(item),
			Aesthetics.Color?.Map(item) ?? Aesthetic.Stroke,
			Aesthetic.StrokeOpacity);

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
	}

	public override void Train(T item)
	{
		Positions.X.Train(item);
		Positions.Y.Train(item);

		Aesthetics.Color?.Train(item);
		Aesthetics.LineType?.Train(item);
	}

	public override void Legend()
	{
		Legend(Aesthetics.Color, value => new Elements.HLine
		{
			Stroke = value,
			StrokeOpacity = Aesthetic.StrokeOpacity,
			StrokeWidth = Aesthetic.StrokeWidth,
			LineType = Aesthetic.LineType
		});

		Legend(Aesthetics.LineType, value => new Elements.HLine
		{
			Stroke = Aesthetic.Stroke,
			StrokeOpacity = Aesthetic.StrokeOpacity,
			StrokeWidth = Aesthetic.StrokeWidth,
			LineType = value
		});
	}

	protected override void Shape(T item, bool flip)
	{
		var color = Aesthetic.Stroke;

		if (Aesthetics.Color is not null)
		{
			color = Aesthetics.Color.Map(item);
			if (string.IsNullOrEmpty(color))
			{
				return;
			}
		}

		var lineType = Aesthetic.LineType;
		if (Aesthetics.LineType is not null)
		{
			lineType = Aesthetics.LineType.Map(item);
		}

		if (!paths.TryGetValue((color, lineType), out var path))
		{
			path = new Shapes.Path
			{
				Aesthetic = new()
				{
					Stroke = color,
					StrokeOpacity = Aesthetic.StrokeOpacity,
					StrokeWidth = Aesthetic.StrokeWidth,
					LineType = lineType
				}
			};

			Layer.Add(path);

			paths[(color, lineType)] = path;
		}

		var x = Positions.X.Map(item);
		if (double.IsNaN(x))
		{
			return;
		}

		var y = Positions.Y.Map(item);
		if (double.IsNaN(y))
		{
			if (piecewise)
			{
				path.Points.Add((x, double.NaN));
			}
		}
		else
		{
			path.Points.Add((x, y));

			if (OnClick is not null || OnMouseOver is not null || OnMouseOut is not null)
			{
				var circle = new Circle
				{
					X = x,
					Y = y,
					Aesthetic = new()
					{
						Radius = 3.0 * Aesthetic.StrokeWidth,
						Fill = "transparent",
						FillOpacity = 0
					},
					OnClick = OnClick is not null ? e => OnClick(item, e) : null,
					OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, x, y, e) : null,
					OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
				};

				Layer.Add(circle);
			}

			if (scale.x)
			{
				Positions.X.Position.Shape(x, x);
			}

			if (scale.y)
			{
				Positions.Y.Position.Shape(y, y);
			}
		}
	}

	public override void Clear()
	{
		base.Clear();

		paths.Clear();
	}
}
