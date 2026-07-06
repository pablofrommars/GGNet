using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Exceptions;

namespace GGNet.Geoms.Candlestick;

internal sealed class Candlestick<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public Candlestick(
		IReadOnlyList<T> source,
		Func<T, TX>? x,
		Func<T, TY> open,
		Func<T, TY> high,
		Func<T, TY> low,
		Func<T, TY> close,
		(bool x, bool y)? scale = null)
		: base(source, scale)
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

	public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

	public Elements.Line Line { get; set; } = default!;
	public Elements.Rectangle Rectangle { get; set; } = default!;

	public override void Init<T1>(Panel<T1, TX, TY> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		if (Selectors.X is null)
		{
			throw new GGNetUserException("X selector is required");
		}

		Positions.X = new PositionMapping<T, TX>(Selectors.X, panel.X);

		Positions.Open = new PositionMapping<T, TY>(Selectors.Open, panel.Y);
		Positions.High = new PositionMapping<T, TY>(Selectors.High, panel.Y);
		Positions.Low = new PositionMapping<T, TY>(Selectors.Low, panel.Y);
		Positions.Close = new PositionMapping<T, TY>(Selectors.Close, panel.Y);
	}

	public override CoordSystem SupportedCoordSystems => CoordSystem.Cartesian;

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

		Func<MouseEventArgs, Task>? onclick = null;
		if (OnClick is not null)
		{
			onclick = e => OnClick(item, e);
		}

		Func<MouseEventArgs, Task>? onmouseover = null;
		if (OnMouseOver is not null)
		{
			onmouseover = e => OnMouseOver(item, e);
		}

		Func<MouseEventArgs, Task>? onmouseout = null;
		if (OnMouseOut is not null)
		{
			onmouseout = e => OnMouseOut(item, e);
		}

		if (close >= open)
		{
			Layer.Add(new Shapes.Line()
			{
				X1 = x - 0.45,
				X2 = x + 0.45,
				Y1 = close,
				Y2 = close,
				Aesthetic = Line,
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x + 0.45,
				X2 = x + 0.45,
				Y1 = close,
				Y2 = open,
				Aesthetic = Line,
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x + 0.45,
				X2 = x - 0.45,
				Y1 = open,
				Y2 = open,
				Aesthetic = Line,
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x - 0.45,
				X2 = x - 0.45,
				Y1 = open,
				Y2 = close,
				Aesthetic = Line,
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x,
				X2 = x,
				Y1 = close,
				Y2 = high,
				Aesthetic = Line,
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x,
				X2 = x,
				Y1 = open,
				Y2 = low,
				Aesthetic = Line,
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
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
				Aesthetic = Line,
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			});

			Layer.Add(new Shapes.Line()
			{
				X1 = x,
				X2 = x,
				Y1 = close,
				Y2 = low,
				Aesthetic = Line,
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			});

			Layer.Add(new Shapes.Rectangle()
			{
				X = x - 0.45,
				Y = close,
				Width = 0.9,
				Height = open - close,
				Aesthetic = Rectangle,
				OnClick = onclick,
				OnMouseOver = onmouseover,
				OnMouseOut = onmouseout
			});
		}

		Positions.X.Position.Shape(x - 0.45, x + 0.45);
		Positions.Close.Position.Shape(low, high);
	}
}
