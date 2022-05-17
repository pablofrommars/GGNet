using GGNet.Common;
using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms.Violin;

internal sealed class Violin<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	private sealed class Comparer : IComparer<(double y, double width)>
	{
		public static readonly Comparer Instance = new();

		public int Compare((double y, double width) a, (double y, double width) b) => a.y.CompareTo(b.y);
	}

	private readonly SortedDictionary<double, Dictionary<string, SortedBuffer<(double y, double width)>>> violins = new();

	private readonly PositionAdjustment position;

	public Violin(
		Source<T> source,
		Func<T, TX>? x,
		Func<T, TY>? y,
		Func<T, double> width,
		IAestheticMapping<T, string>? fill = null,
		PositionAdjustment position = PositionAdjustment.Identity,
		(bool x, bool y)? scale = null,
		bool inherit = true)
		: base(source, scale, inherit)
	{
		Selectors = new()
		{
			X = x,
			Y = y,
			Width = width
		};

		Aesthetics = new()
		{
			Fill = fill
		};

		this.position = position;
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Aesthetics<T> Aesthetics { get; }

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

		Aesthetics.Fill ??= panel.Data.Aesthetics.Fill as IAestheticMapping<T, string>;
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
			Alpha = Aesthetic.Alpha,
			Color = Aesthetic.Color,
			Width = Aesthetic.Width
		});
	}

	protected override void Shape(T item, bool flip)
	{
		var fill = Aesthetic.Fill;

		if (Aesthetics.Fill is not null)
		{
			fill = Aesthetics.Fill.Map(item);
			if (string.IsNullOrEmpty(fill))
			{
				return;
			}
		}

		var x = Positions.X.Map(item);
		var y = Positions.Y.Map(item);

		SortedBuffer<(double y, double width)>? violin;

		if (violins.TryGetValue(x, out var xviolins))
		{
			if (!xviolins.TryGetValue(fill, out violin))
			{
				violin = new(64, 1, Comparer.Instance);
				xviolins[fill] = violin;
			}
		}
		else
		{
			violin = new(64, 1, Comparer.Instance);

			violins[x] = new()
			{
				[fill] = violin
			};
		}

		violin.Add((y, Selectors.Width(item)));
	}

	private void Identity()
	{
		foreach (var xviolins in violins)
		{
			var c = xviolins.Key;

			foreach (var violin in xviolins.Value)
			{
				var xmin = c;
				var xmax = c;

				var n = violin.Value.Count;
				var longitude = new double[2 * n];
				var latitude = new double[2 * n];
				var j = 0;

				for (var i = 0; i < n; i++)
				{
					var (y, width) = violin.Value[i];

					var x = c - 0.45 * width;
					if (x < xmin)
					{
						xmin = x;
					}

					longitude[j] = x;
					latitude[j] = y;

					j++;
				}

				for (var i = n - 1; i >= 0; i--)
				{
					var (y, width) = violin.Value[i];

					var x = c + 0.45 * width;
					if (xmax < x)
					{
						xmax = x;
					}

					longitude[j] = x;
					latitude[j] = y;

					j++;
				}

				var poly = new Shapes.Polygon
				{
					Path = new()
					{
						Longitude = longitude,
						Latitude = latitude
					},
					Aesthetic = new()
					{
						Fill = violin.Key,
						Alpha = Aesthetic.Alpha,
						Color = Aesthetic.Color,
						Width = Aesthetic.Width
					}
				};

				Layer.Add(poly);

				Positions.X.Position.Shape(xmin, xmax);
				Positions.Y.Position.Shape(violin.Value[0].y, violin.Value[n - 1].y);
			}
		}
	}

	private void Dodge()
	{
		var delta = 0.8;

		if (violins.Count > 1)
		{
			var d = double.MaxValue;

			for (var i = 1; i < violins.Count; i++)
			{
				d = Math.Min(d, violins.ElementAt(i).Key - violins.ElementAt(i - 1).Key);
			}

			delta *= d;
		}

		foreach (var xviolins in violins)
		{
			var n = xviolins.Value.Count;

			var w = delta / n;
			var c = xviolins.Key - delta / 2.0 + w / 2.0;

			foreach (var violin in xviolins.Value)
			{
				var xmin = c;
				var xmax = c;

				var N = violin.Value.Count;
				var longitude = new double[2 * N];
				var latitude = new double[2 * N];
				var j = 0;

				for (var i = 0; i < N; i++)
				{
					var (y, width) = violin.Value[i];

					var x = c - 0.4 * w * width;
					if (x < xmin)
					{
						xmin = x;
					}

					longitude[j] = x;
					latitude[j] = y;

					j++;
				}

				for (var i = N - 1; i >= 0; i--)
				{
					var (y, width) = violin.Value[i];

					var x = c + 0.4 * w * width;
					if (xmax < x)
					{
						xmax = x;
					}

					longitude[j] = x;
					latitude[j] = y;

					j++;
				}

				var poly = new Polygon
				{
					Path = new()
					{
						Longitude = longitude,
						Latitude = latitude
					},
					Aesthetic = new()
					{
						Fill = violin.Key,
						Alpha = Aesthetic.Alpha,
						Color = Aesthetic.Color,
						Width = Aesthetic.Width
					}
				};

				Layer.Add(poly);

				if (scale.x)
				{
					Positions.X.Position.Shape(xmin, xmax);
				}

				if (scale.y)
				{
					Positions.Y.Position.Shape(violin.Value[0].y, violin.Value[N - 1].y);
				}

				c += w;
			}
		}
	}

	protected override void Set(bool flip)
	{
		if (position == PositionAdjustment.Identity)
		{
			Identity();
		}
		else if (position == PositionAdjustment.Dodge)
		{
			Dodge();
		}
		else
		{
			throw new NotImplementedException();
		}
	}

	public override void Clear()
	{
		base.Clear();

		violins.Clear();
	}
}