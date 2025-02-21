using GGNet.Data;
using GGNet.Facets;

namespace GGNet.Geoms.OHCL;

internal sealed class OHLC<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public OHLC(
		IReadOnlyList<T> source,
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

		/*
		var aes = new Elements.Line
		{
			Width = width,
			Color = color,
			Alpha = alpha,
			LineType = LineType.Solid
		};

		mapping = new Mapping
		{
			Open = open,
			High = high,
			Low = low,
			Close = close
		};

		vstrip = new VStrip<T>(
			panel,
			x: o => panel.Data.Scales.X.Map(o) - 0.5,
			y: mapping.Close,
			width: o => 1.0,
			layer: Layer)
		{
			OnMouseOver = (item, _) =>
			{
				if (vtrack)
				{
					Panel.Component.Plot.VTrack.Show(panel.Data.Scales.X.Map(item));
				}

				if (ylabel)
				{
					Panel.Component.YLabel.Show(mapping.Close(item));
				}

				return Task.CompletedTask;
			},
			OnMouseOut = (item, _) =>
			{
				if (vtrack)
				{
					Panel.Component.Plot.VTrack.Hide();
				}

				if (ylabel)
				{
					Panel.Component.YLabel.Hide();
				}

				return Task.CompletedTask;
			}
		};

		this.open = new Segment<T>(
			panel,
			x: o => panel.Data.Scales.X.Map(o) - 0.5,
			xend: o => panel.Data.Scales.X.Map(o),
			y: mapping.Open,
			yend: mapping.Open,
			layer: Layer)
		{
			OnClick = onclick,
			OnMouseOver = OnMouseOver,
			OnMouseOut = OnMouseOut,
			Aesthetic = aes
		};

		range = new Segment<T>(
			panel,
			x: o => panel.Data.Scales.X.Map(o),
			xend: o => panel.Data.Scales.X.Map(o),
			y: mapping.High,
			yend: mapping.Low,
			layer: Layer)
		{
			OnClick = onclick,
			OnMouseOver = OnMouseOver,
			OnMouseOut = OnMouseOut,
			Aesthetic = aes
		};

		this.close = new Segment<T>(
			panel,
			x: o => panel.Data.Scales.X.Map(o),
			xend: o => panel.Data.Scales.X.Map(o) + 0.5,
			y: mapping.Close,
			yend: mapping.Close,
			layer: Layer)
		{
			OnClick = onclick,
			OnMouseOver = OnMouseOver,
			OnMouseOut = OnMouseOut,
			Aesthetic = aes
		};
		*/
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Positions<T> Positions { get; } = new();

	public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

	public Elements.Line Aesthetic { get; set; } = default!;

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

		Layer.Add(new Shapes.Line()
		{
			X1 = x - 0.5,
			X2 = x,
			Y1 = open,
			Y2 = open,
			Aesthetic = Aesthetic,
			OnMouseOver = onmouseover,
			OnMouseOut = onmouseout
		});

		Layer.Add(new Shapes.Line()
        {
            X1 = x,
            X2 = x,
            Y1 = low,
            Y2 = high,
			Aesthetic = Aesthetic,
			OnMouseOver = onmouseover,
			OnMouseOut = onmouseout
        });

		Layer.Add(new Shapes.Line()
        {
            X1 = x,
            X2 = x + 0.5,
            Y1 = close,
            Y2 = close,
			Aesthetic = Aesthetic,
			OnMouseOver = onmouseover,
			OnMouseOut = onmouseout

        });

		Positions.X.Position.Shape(x - 0.5, x + 0.5);
		/*
		Positions.Open.Position.Shape(open, open);
		Positions.High.Position.Shape(high, high);
		Positions.Low.Position.Shape(low, low);
		Positions.Close.Position.Shape(close, close);
		*/
		Positions.Close.Position.Shape(low, high);
	}
}