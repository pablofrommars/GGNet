using GGNet.Data;
using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms.Rectangle;

internal sealed class Rectangle<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public Rectangle(
		Source<T> source,
		Func<T, TX> x,
		Func<T, TY> y,
		Func<T, double> width,
		Func<T, double> height,
		(bool x, bool y)? scale = null)
		: base(source, scale, false)
	{
		Selectors = new()
		{
			X = x,
			Y = y,
			Width = width,
			Height = height
		};
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Positions<T> Positions { get; } = new();

	public Elements.Rectangle Aesthetic { get; set; } = default!;

	public override void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		Positions.X = XMapping(Selectors.X!, panel.X);
		Positions.Y = YMapping(Selectors.Y!, panel.Y);
	}

	public override void Train(T item)
	{
		Positions.X.Train(item);
		Positions.Y.Train(item);
	}

	protected override void Shape(T item, bool flip)
	{
		var x = Positions.X.Map(item);
		var y = Positions.Y.Map(item);
		var width = Selectors.Width(item);
		var height = Selectors.Height(item);

		var rectangle = new GGNet.Shapes.Rectangle
		{
			X = x,
			Y = y,
			Width = width,
			Height = height,
			Aesthetic = Aesthetic
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
