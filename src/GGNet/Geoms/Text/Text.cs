using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Exceptions;

namespace GGNet.Geoms.Text;

internal sealed class Text<T, TX, TY, TT> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public Text(
		IReadOnlyList<T> source,
		Func<T, TX>? x,
		Func<T, TY>? y,
		Func<T, double>? angle,
		Func<T, TT>? text,
		IAestheticMapping<T, string>? color = null,
		(bool x, bool y)? scale = null)
		: base(source, scale)
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

	public required Elements.Text Aesthetic { get; set; }

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
	}

	public override void Train(T item)
	{
		Positions.X.Train(item);
		Positions.Y.Train(item);
	}

	protected override void Shape(T item)
	{
		if (Selectors.Text is null)
		{
			return;
		}

		var value = Formats.InvariantText.Of(Selectors.Text(item));
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

		var width = value.Width(Aesthetic.FontSize);
		var height = value.Height(Aesthetic.FontSize);

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
