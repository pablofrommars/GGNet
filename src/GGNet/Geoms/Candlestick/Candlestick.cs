using GGNet.Data;
using GGNet.Facets;

namespace GGNet.Geoms.Candlestick;

internal sealed class Candlestick<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public Candlestick(
		Source<T> source,
		Func<T, TX>? x,
		Func<T, TY> open,
		Func<T, TY> high,
		Func<T, TY> low,
		Func<T, TY> close,
		(bool x, bool y)? scale = null)
		: base(source, scale, false)
	{
		Selectors = new()
		{
			X = x,
			Open = open,
			High = high,
			Low = low,
			Close = close
		};
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Positions<T> Positions { get; } = new();

	public Elements.Line Line { get; set; } = default!;
	public Elements.Rectangle Rectangle { get; set; } = default!;

	public override void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		if (Selectors.X == null)
		{
			Positions.X = XMapping(panel.Data.Selectors.X!, panel.X);
		}
		else
		{
			Positions.X = XMapping(Selectors.X, panel.X);
		}

		Positions.Open = YMapping(Selectors.Open, panel.Y);
		Positions.High = YMapping(Selectors.High, panel.Y);
		Positions.Low = YMapping(Selectors.Low, panel.Y);
		Positions.Close = YMapping(Selectors.Close, panel.Y);
	}

	public override void Train(T item)
	{
		Positions.X.Train(item);
		Positions.Open.Train(item);
		Positions.High.Train(item);
		Positions.Low.Train(item);
		Positions.Close.Train(item);
	}

	protected override void Shape(T item, bool flip)
	{
		var x = Positions.X.Map(item);

		var open = Positions.Open.Map(item);
		var high = Positions.High.Map(item);
		var low = Positions.Low.Map(item);
		var close = Positions.Close.Map(item);

		if (close >= open)
		{
			Layer.Add(new Shapes.Line()
			{
				X1 = x - 0.45,
				X2 = x + 0.45,
				Y1 = close,
				Y2 = close,
				Aesthetic = Line
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x + 0.45,
				X2 = x + 0.45,
				Y1 = close,
				Y2 = open,
				Aesthetic = Line
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x + 0.45,
				X2 = x - 0.45,
				Y1 = open,
				Y2 = open,
				Aesthetic = Line
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x - 0.45,
				X2 = x - 0.45,
				Y1 = open,
				Y2 = close,
				Aesthetic = Line
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x,
				X2 = x,
				Y1 = close,
				Y2 = high,
				Aesthetic = Line
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x,
				X2 = x,
				Y1 = open,
				Y2 = low,
				Aesthetic = Line
			});
		}
		else
		{
			Layer.Add(new Shapes.Line()
			{
				X1 = x,
				X2 = x,
				Y1 = open,
				Y2 = high,
				Aesthetic = Line
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x,
				X2 = x,
				Y1 = close,
				Y2 = low,
				Aesthetic = Line
			});

			Layer.Add(new Shapes.Rectangle()
			{
				X = x - 0.45,
				Y = close,
				Width = 0.9,
				Height = open - close,
				Aesthetic = Rectangle
			});
		}

		Positions.X.Position.Shape(x - 0.45, x + 0.45);
		Positions.Close.Position.Shape(low, high);
	}
}