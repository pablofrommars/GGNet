using GGNet.Buffers;
using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;
using GGNet.Exceptions;

namespace GGNet.Geoms.Bar;

internal sealed class Bar<T, TX, TY> : Geom<T, TX, TY>
  where TX : struct
  where TY : struct
{
	private sealed class Comparer : IComparer<(double x, List<(T item, string fill, double value)> y)>
	{
		public int Compare((double x, List<(T item, string fill, double value)> y) x, (double x, List<(T item, string fill, double value)> y) y) => x.x.CompareTo(y.x);

		public static readonly Comparer Instance = new();
	}

	private readonly SortedBuffer<(double x, List<(T item, string fill, double value)> y)> bars = new(Comparer.Instance);

	private readonly PositionAdjustment position;
	private readonly double width;

	private readonly bool animation;

	private bool flip;

	public Bar(
	  IReadOnlyList<T> source,
	  Func<T, TX>? x,
	  Func<T, TY>? y,
	  IAestheticMapping<T, string>? fill = null,
	  Func<T, RenderFragment>? tooltip = null,
	  PositionAdjustment position = PositionAdjustment.Stack,
	  double width = 0.9,
	  bool animation = false,
	  (bool x, bool y)? scale = null)
	  : base(source, scale)
	{
		Selectors = new()
		{
			X = x,
			Y = y,
			Tooltip = tooltip
		};

		Aesthetics = new()
		{
			Fill = fill
		};

		this.position = position;
		this.width = width;

		this.animation = animation;
	}

	public Selectors<T, TX, TY> Selectors { get; }

	public Aesthetics<T> Aesthetics { get; }

	public Positions<T> Positions { get; } = new();

	public Func<T, MouseEventArgs, Task>? OnClick { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOver { get; set; }

	public Func<T, MouseEventArgs, Task>? OnMouseOut { get; set; }

	private Func<T, double, double, MouseEventArgs, Task>? onMouseOver;

	public required Elements.Rectangle Aesthetic { get; set; }

	public override void Init<T1>(Panel<T1, TX, TY> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		if (Selectors.X is null)
		{
			throw new GGNetUserException("X selector is required");
		}

		if (Selectors.Y is null)
		{
			throw new GGNetUserException("Y selector is required");
		}

		// Flip is Bar's statistical-axis choice, captured at wiring time: the
		// grouping key and the stacking axis swap roles (selector/scale pairs
		// stay intact) and AddRect transposes the emitted rectangles to match.
		flip = panel.Data.Flip;

		if (flip)
		{
			Positions.X = new PositionMapping<T, TY>(Selectors.Y, panel.Y);
			Positions.Y = new PositionMapping<T, TX>(Selectors.X, panel.X);
		}
		else
		{
			Positions.X = new PositionMapping<T, TX>(Selectors.X, panel.X);
			Positions.Y = new PositionMapping<T, TY>(Selectors.Y, panel.Y);
		}

		if (OnMouseOver is null && OnMouseOut is null && Selectors.Tooltip is not null)
		{
			onMouseOver = (item, x, y, _) =>
			{
				panel.Component?.Tooltip?.Show(
			x,
			y,
			0,
			Selectors.Tooltip(item),
			Aesthetics.Fill?.Map(item) ?? Aesthetic.Fill,
			Aesthetic.FillOpacity);

				return Task.CompletedTask;
			};

			OnMouseOut = (_, __) =>
			{
				panel.Component?.Tooltip?.Hide();

				return Task.CompletedTask;
			};
		}
		else if (OnMouseOver is not null)
		{
			onMouseOver = (item, _, __, e) => OnMouseOver(item, e);
		}
	}

	public override CoordSystem SupportedCoordSystems => CoordSystem.Cartesian;

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

	protected override void Shape(T item)
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

		var exist = false;

		for (var i = 0; i < bars.Count; i++)
		{
			var bar = bars[i];
			if (bar.x == x)
			{
				bar.y.Add((item, fill, y));
				exist = true;
				break;
			}
		}

		if (!exist)
		{
			var bar = new List<(T item, string fill, double value)>();
			bar.Add((item, fill, y));
			bars.Add((x, bar));
		}
	}

	private void AddRect(T item, string fill, double x, double y, double w, double h, double anchorX, double anchorY)
	{
		var aesthetic = new Elements.Rectangle
		{
			Fill = fill,
			FillOpacity = Aesthetic.FillOpacity,
			Stroke = Aesthetic.Stroke,
			StrokeWidth = Aesthetic.StrokeWidth,
		};

		Layer.Add(flip
			? new Rectangle
			{
				Classes = animation ? "animate-bar" : string.Empty,
				X = y,
				Y = x,
				Width = h,
				Height = w,
				Aesthetic = aesthetic,
				OnClick = OnClick is not null ? e => OnClick(item, e) : null,
				OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, anchorY, anchorX, e) : null,
				OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
			}
			: new Rectangle
			{
				Classes = animation ? "animate-bar" : string.Empty,
				X = x,
				Y = y,
				Width = w,
				Height = h,
				Aesthetic = aesthetic,
				OnClick = OnClick is not null ? e => OnClick(item, e) : null,
				OnMouseOver = onMouseOver is not null ? e => onMouseOver(item, anchorX, anchorY, e) : null,
				OnMouseOut = OnMouseOut is not null ? e => OnMouseOut(item, e) : null
			});
	}

	private double Delta()
	{
		var delta = width;

		if (bars.Count > 1)
		{
			var d = double.MaxValue;

			for (var i = 1; i < bars.Count; i++)
			{
				d = Math.Min(d, bars[i].x - bars[i - 1].x);
			}

			delta *= d;
		}

		return delta;
	}

	private void Stack()
	{
		var delta = Delta();

		for (var i = 0; i < bars.Count; i++)
		{
			var (x, y) = bars[i];
			var sum = 0.0;

			for (var j = y.Count - 1; j >= 0; j--)
			{
				var (item, fill, value) = y[j];

				AddRect(item, fill, x - delta / 2.0, sum, delta, value, x, sum + value);

				sum += value;
			}

			if (scale.x)
			{
				Positions.X.Position.Shape(x - delta, x + delta);
			}

			if (scale.y)
			{
				Positions.Y.Position.Shape(0, sum);
			}
		}
	}

	private void Dodge()
	{
		var delta = Delta();

		for (var i = 0; i < bars.Count; i++)
		{
			var bar = bars[i];
			var n = bar.y.Count;

			var w = delta / n;
			var x = bar.x - delta / 2.0;

			for (var j = 0; j < n; j++)
			{
				var (item, fill, value) = bar.y[j];

				AddRect(item, fill, x, value >= 0 ? 0 : value, w, Math.Abs(value), x + w / 2.0, value);

				if (scale.x)
				{
					Positions.X.Position.Shape(x, x + w);
				}

				if (scale.y)
				{
					if (value >= 0)
					{
						Positions.Y.Position.Shape(0, value);
					}
					else
					{
						Positions.Y.Position.Shape(value, 0);
					}
				}

				x += w;
			}
		}
	}

	protected override void Set()
	{
		if (position == PositionAdjustment.Stack)
		{
			Stack();
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

		bars.Clear();
	}
}
