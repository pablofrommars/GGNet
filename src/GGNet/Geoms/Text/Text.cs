using GGNet.Common;
using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms.Text;

internal sealed class Text<T, TX, TY, TT> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public Text(
		Source<T> source,
		Func<T, TX>? x,
		Func<T, TY>? y,
		Func<T, double>? angle,
		Func<T, TT>? text,
		IAestheticMapping<T, string>? color = null,
		(bool x, bool y)? scale = null,
		bool inherit = true)
		: base(source, scale, inherit)
	{
		Selectors = new()
		{
			X = x,
			Y = y,
			Angle = angle,
			Text = text
		};

		Aesthetics = new()
		{
			Color = color
		};
	}

	public Selectors<T, TX, TY, TT> Selectors { get; }

	public Aesthetics<T> Aesthetics { get; }

	public Positions<T> Positions { get; } = new();

	public Elements.Text Aesthetic { get; set; } = default!;

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

		if (Selectors.Y is null)
		{
			Positions.Y = YMapping(panel.Data.Selectors.Y!, panel.Y);
		}
		else
		{
			Positions.Y = YMapping(Selectors.Y, panel.Y);
		}

		if (!inherit)
		{
			return;
		}

		Aesthetics.Color ??= panel.Data.Aesthetics.Color as IAestheticMapping<T, string>;
	}

	public override void Train(T item)
	{
		Positions.X.Train(item);
		Positions.Y.Train(item);
	}

	protected override void Shape(T item, bool flip)
	{
		if (Selectors.Text is null)
		{
			return;
		}
		
		var value = Selectors.Text(item)?.ToString();
		if (string.IsNullOrEmpty(value))
		{
			return;
		}

		var color = Aesthetic.Color;
		if (Aesthetics.Color is not null)
		{
			color = Aesthetics.Color.Map(item);
			if (string.IsNullOrEmpty(color))
			{
				return;
			}
		}

		var angle = Aesthetic.Angle;
		if (Selectors.Angle is not null)
		{
			angle = Selectors.Angle(item);
		}

		var x = Positions.X.Map(item);
		var y = Positions.Y.Map(item);

		var width = value.Width(Aesthetic.Size);
		var height = value.Height(Aesthetic.Size);

		var text = new Shapes.Text
		{
			X = x,
			Y = y,
			Width = width,
			Height = height,
			Value = value,
			Aesthetic = Aesthetic with
			{
				Color = color,
				Angle = angle
			}
		};

		Layer.Add(text);

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