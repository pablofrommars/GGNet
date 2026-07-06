using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Exceptions;

namespace GGNet.Geoms.RidgeLine;

internal sealed class RidgeLine<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
	private readonly Dictionary<(double y, string fille), Shapes.Area> areas = [];

	public RidgeLine(
	  Source<T> source,
	  Func<T, TX>? x,
	  Func<T, TY>? y,
	  Func<T, double> height,
	  IAestheticMapping<T, string>? fill = null,
	  (bool x, bool y)? scale = null)
	  : base(source, scale)
	{
		Selectors = new()
		{
			X = x,
			Y = y,
			Height = height
		};

		Aesthetics = new()
		{
			Fill = fill
		};
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Aesthetics<T> Aesthetics { get; }

	public Positions<T> Positions { get; } = new();

	public required Elements.Rectangle Aesthetic { get; set; }

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
		Shapes.Area area;

		var x = Positions.X.Map(item);
		var y = Positions.Y.Map(item);

		if (Aesthetics.Fill is null)
		{
			var key = (y, Aesthetic.Fill);

			if (!areas.TryGetValue(key, out area))
			{
				area = new Shapes.Area { Aesthetic = Aesthetic };

				Layer.Add(area);

				areas[key] = area;
			}
		}
		else
		{
			var fill = Aesthetics.Fill.Map(item);
			if (string.IsNullOrEmpty(fill))
			{
				return;
			}

			var key = (y, fill);

			if (!areas.TryGetValue(key, out area))
			{
				area = new Shapes.Area
				{
					Aesthetic = new()
					{
						Fill = fill,
						FillOpacity = Aesthetic.FillOpacity
					}
				};

				Layer.Add(area);

				areas[key] = area;
			}
		}

		var height = Selectors.Height(item);

		area.Points.Add((x, y, y + height));

		if (scale.x)
		{
			Positions.X.Position.Shape(x, x);
		}

		if (scale.y)
		{
			Positions.Y.Position.Shape(y, y + height);
		}
	}

	public override void Clear()
	{
		base.Clear();

		areas.Clear();
	}
}
