using GGNet.Data;
using GGNet.Facets;

namespace GGNet.Geoms.Volume;

internal sealed class Volume<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public Volume(
		Source<T> source,
		Func<T, TX>? x,
		Func<T, TY> volume)
		: base(source, null, false)
	{
		Selectors = new()
		{
			X = x,
			Volume = volume
		};

		/*
		vstrip = new VStrip<T>(
			panel,
			x: o => panel.Data.Scales.X.Map(o) - 0.5,
			y: mapping.Volume,
			width: o => 1.0,
			layer: Layer)
		{
			OnMouseOver = OnMouseOver,
			OnMouseOut = OnMouseOut
		};

		rectangle = new Rectangle<T>(
			panel,
			x: o => panel.Data.Scales.X.Map(o) - 0.45,
			y: _ => 0.0,
			width: _ => 0.9,
			height: mapping.Volume,
			layer: Layer)
		{
			OnClick = onclick,
			OnMouseOver = OnMouseOver,
			OnMouseOut = OnMouseOut,
			Aesthetic = new Elements.Rectangle
			{ 
				Fill = color,
				Alpha = alpha
			}
		};
		*/
	}

	public Selectors<T, TX, TY> Selectors { get; } = new();

	public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

	public Positions<T> Positions { get; } = new();

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

		Positions.Volume = YMapping(Selectors.Volume, panel.Y);
	}

	public override void Train(T item)
	{
		Positions.X.Train(item);
		Positions.Volume.Train(item);
	}

	protected override void Shape(T item, bool flip)
	{
		var x = Positions.X.Map(item);
		var volume = Positions.Volume.Map(item);

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

		Layer.Add(new Shapes.Rectangle
        {
            X = x - 0.45,
            Y = 0,
            Width = 0.9,
            Height = volume,
            Aesthetic = Aesthetic,
            OnMouseOver = onmouseover,
            OnMouseOut = onmouseout
        });

		Positions.X.Position.Shape(x - 0.45, x + 0.45);
		Positions.Volume.Position.Shape(0, volume);
	}
}