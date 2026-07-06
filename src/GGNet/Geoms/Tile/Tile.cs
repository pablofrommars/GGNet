using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;
using GGNet.Exceptions;

namespace GGNet.Geoms.Tile;

internal sealed class Tile<T, TX, TY> : Geom<T, TX, TY>
where TX : struct
where TY : struct
{
	public Tile(
	  IReadOnlyList<T> source,
	  Func<T, TX> x,
	  Func<T, TY> y,
	  Func<T, double> width,
	  Func<T, double> height,
	  IAestheticMapping<T, string>? fill = null,
	  Func<T, RenderFragment>? tooltip = null,
	  (bool x, bool y)? scale = null)
	  : base(source, scale)
	{
		Selectors = new()
		{
			X = x,
			Y = y,
			Width = width,
			Height = height,
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
					Aesthetics.Fill?.Map(item) ?? Aesthetic.Fill,
					Aesthetic.FillOpacity);

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

	public override CoordSystem SupportedCoordSystems => CoordSystem.Cartesian;

	public override void Train(T item)
	{
		Positions.X.Train(item);
		Positions.Y.Train(item);

		Aesthetics.Fill?.Train(item);
	}

	public override void Legend()
	{
		Legend(Aesthetics.Fill, value => new Elements.Rectangle
		{
			Fill = value,
			FillOpacity = Aesthetic.FillOpacity
		});
	}

	protected override void Shape(T item, bool flip)
	{
		var x = Positions.X.Map(item);
		var y = Positions.Y.Map(item);
		var width = Selectors.Width(item);
		var height = Selectors.Height(item);

		var fill = Aesthetic.Fill;
		if (Aesthetics.Fill is not null)
		{
			fill = Aesthetics.Fill.Map(item);
			if (string.IsNullOrEmpty(fill))
			{
				return;
			}
		}

		var rectangle = new Rectangle
		{
			X = x,
			Y = y,
			Width = width,
			Height = height,
			Aesthetic = new()
			{
				Fill = fill,
				FillOpacity = Aesthetic.FillOpacity,
				Stroke = Aesthetic.Stroke,
				StrokeWidth = Aesthetic.StrokeWidth
			},
			OnClick = OnClick is not null ? e => OnClick(item, e) : null,
			OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, x + width / 2.0, y + height, e) : null,
			OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
		};

		Layer.Add(rectangle);

		if (scale.x)
		{
			Positions.X.Position.Shape(x, x + width);
		}

		if (scale.y)
		{
			Positions.Y.Position.Shape(y, y + height);
		}
	}
}
