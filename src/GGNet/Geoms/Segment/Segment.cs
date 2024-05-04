using GGNet.Data;
using GGNet.Facets;

using static System.Math;

namespace GGNet.Geoms.Segment;

internal sealed class Segment<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public Segment(
		Source<T> source,
		Func<T, TX> x,
		Func<T, TX> xend,
		Func<T, TY> y,
		Func<T, TY> yend,
		(bool x, bool y)? scale = null)
		: base(source, scale, false)
	{
		Selectors = new()
		{
			X = x,
			XEnd = xend,
			Y = y,
			YEnd = yend
		};
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

	public Positions<T> Positions { get; } = new();

	public Elements.Line Aesthetic { get; set; } = default!;

	public override void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		Positions.X = XMapping(Selectors.X!, panel.X);
		Positions.XEnd = XMapping(Selectors.XEnd!, panel.X);
		Positions.Y = YMapping(Selectors.Y!, panel.Y);
		Positions.YEnd = YMapping(Selectors.YEnd!, panel.Y);
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
      OnMouseOver = OnMouseOver is not null ? e => OnMouseOver(item, e) : null,
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
