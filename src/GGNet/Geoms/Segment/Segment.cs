using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;

using static System.Math;

namespace GGNet.Geoms.Segment;

internal sealed class Segment<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public Segment(
		IReadOnlyList<T> source,
		Func<T, TX> x,
		Func<T, TX> xend,
		Func<T, TY> y,
		Func<T, TY> yend,
		Func<T, RenderFragment>? tooltip = null,
		(bool x, bool y)? scale = null)
		: base(source, scale)
	{
		Selectors = new()
		{
			X = x,
			XEnd = xend,
			Y = y,
			YEnd = yend,
			Tooltip = tooltip
		};
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

	private Func<T, double, double, MouseEventArgs, Task>? onMouseOver;

	public Positions<T> Positions { get; } = new();

	public Elements.Line Aesthetic { get; set; } = default!;

	public override void Init<T1>(Panel<T1, TX, TY> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		Positions.X = new PositionMapping<T, TX>(Selectors.X!, panel.X);
		Positions.XEnd = new PositionMapping<T, TX>(Selectors.XEnd!, panel.X);
		Positions.Y = new PositionMapping<T, TY>(Selectors.Y!, panel.Y);
		Positions.YEnd = new PositionMapping<T, TY>(Selectors.YEnd!, panel.Y);

		if (OnMouseOver is null && OnMouseOut is null && Selectors.Tooltip is not null)
		{
			onMouseOver = (item, x, y, _) =>
			{
				panel.Component?.Tooltip?.Show(
					x,
					y,
					0,
					Selectors.Tooltip(item),
					Aesthetic.Stroke,
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
		Positions.XEnd.Train(item);
		Positions.Y.Train(item);
		Positions.YEnd.Train(item);
	}

	protected override void Shape(T item, bool flip)
	{
		var x = Positions.X.Map(item);
		var xend = Positions.XEnd.Map(item);
		var y = Positions.Y.Map(item);
		var yend = Positions.YEnd.Map(item);

		var line = new Shapes.Line
		{
			X1 = x,
			X2 = xend,
			Y1 = y,
			Y2 = yend,
			Aesthetic = Aesthetic,
			OnClick = OnClick is not null ? e => OnClick(item, e) : null,
			OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, (x + xend) / 2.0, (y + yend) / 2.0, e) : null,
			OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
		};

		Layer.Add(line);

		if (scale.x)
		{
			Positions.X.Position.Shape(Min(x, xend), Max(x, xend));
		}

		if (scale.y)
		{
			Positions.Y.Position.Shape(Min(y, yend), Max(y, yend));
		}
	}
}
