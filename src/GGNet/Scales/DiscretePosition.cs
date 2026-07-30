using GGNet.Buffers;
using GGNet.Formats;
using GGNet.Transformations;

namespace GGNet.Scales;

internal class DiscretePosition<T> : Position<T>
  where T : struct
{
	protected readonly SortedBuffer<T> values = new();

	protected readonly IFormatter<T> formatter;
	private readonly double offset;
	private readonly bool hide;

	public DiscretePosition(ITransformation<T>? transformation = null,
	  (T? min, T? max)? limits = null,
	  (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
	  IFormatter<T>? formatter = null,
	  double offset = 0.0,
	  bool hide = false)
	  : base(transformation, expand ?? (0.0, 0.6, 0, 0.6))
	{
		Limits = limits ?? (null, null);
		this.formatter = formatter ?? Standard<T>.Instance;
		this.offset = offset;
		this.hide = hide;
	}

	public override Guide Guide => Guide.None;

	public override void Train(T key) => values.Add(key);

	protected virtual void Labeling(int start, int last)
	{
		var labels = new (double value, string label)[values.Count];

		for (var i = 0; i < values.Count; i++)
		{
			labels[i] = (i + offset, formatter.Format(values[i]));
		}

		Labels = labels;

		if (!hide)
		{
			var breaks = new double[values.Count];

			for (var i = 0; i < values.Count; i++)
			{
				breaks[i] = i + offset;
			}

			Breaks = breaks;
		}
	}

	public override void Commit(bool grid)
	{
		var min = _min ?? 0.0;
		var max = _max ?? 0.0;

		var start = 0;
		var end = values.Count;

		if (Limits.min.HasValue)
		{
			var index = values.IndexOf(Limits.min.Value);
			if (index >= 0)
			{
				min = index;
				start = index;
			}
		}

		if (Limits.max.HasValue)
		{
			var index = values.IndexOf(Limits.max.Value);
			if (index >= 0)
			{
				max = index;
				end = index + 1;
			}
		}

		if (CommitViewRange())
		{
			// Snap the label window to the categories inside the view.
			start = Math.Max(0, (int)Math.Ceiling(Range.min));
			end = Math.Min(values.Count, (int)Math.Floor(Range.max) + 1);
		}
		else
		{
			SetRange(min, max);
		}

		if (grid)
		{
			Labeling(start, end);
		}
	}

	// No persistent trained state: values rebuild every pass. Reintroduce
	// persistence only if a streaming case proves hot, as a documented
	// optimization — the append-across-passes invariant hid a SortedBuffer
	// corruption bug once already.
	public override void Clear()
	{
		base.Clear();

		values.Clear();
	}

	public override T? Unmap(double value)
	{
		var index = (int)Math.Round(value);

		if (index < 0 || index >= values.Count)
		{
			return null;
		}

		return values[index];
	}

	public override string? Label(T value) => formatter.Format(value);

	public override double Map(T key)
	{
		var index = values.IndexOf(key);
		if (index < 0)
		{
			return double.NaN;
		}

		return index;
	}
}
