using GGNet.Buffers;
using GGNet.Transformations;

namespace GGNet.Scales;

internal class DateTimePosition : Position<LocalDateTime>
{
	private static readonly LocalTimePattern timePattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm");
	private static readonly LocalDatePattern datePattern = LocalDatePattern.CreateWithInvariantCulture("MM/dd");
	private static readonly Period sampling = Period.FromMinutes(1);

	protected readonly SortedBuffer<LocalDateTime> values = new();

	public DateTimePosition(ITransformation<LocalDateTime>? transformation = null,
		(LocalDateTime? min, LocalDateTime? max)? limits = null,
		(double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
		: base(transformation, expand ?? (0.0, 5, 0, 5))
	{
		Limits = limits ?? (null, null);
	}

	public override Guide Guide => Guide.None;

	private LocalDate? first;
	private LocalDate? last;

	private LocalDateTime? min;
	private LocalDateTime? max;

	public override void Train(LocalDateTime key)
	{
		if (values.Count > 0)
		{
			var date = key.Date;
			if (first > date)
			{
				first = date;
			}

			if (last < date)
			{
				last = date;
			}

			if (min > key)
			{
				min = key;
			}

			if (max < key)
			{
				max = key;
			}
		}
		else
		{
			first = key.Date;
			last = key.Date;
			min = key;
			max = key;
		}

		// Every observed key is retained. Minute sampling used to be interpolated here, forward
		// of the running maximum, which silently dropped any key that arrived out of order.
		values.Add(key);

		sampled = false;
	}

	// The pipeline shapes before it commits — geoms call Map while the scale is still being
	// read for the first time — so the derived grid is finalized on first read, not at Commit.
	// Deriving it at Commit left marks on observed-only indices while the axis used sampled ones.
	private bool sampled;

	private void EnsureSampled()
	{
		if (sampled)
		{
			return;
		}

		sampled = true;

		Sample();
	}

	// One 1-minute grid per observed day, spanning that day's observed extent. Derived from the
	// sorted buffer at commit time, so the result depends on the data and not on arrival order.
	private void Sample()
	{
		if (values.Count < 2)
		{
			return;
		}

		var samples = new List<LocalDateTime>();

		var i = 0;
		while (i < values.Count)
		{
			var date = values[i].Date;

			var j = i;
			while (j < values.Count && values[j].Date == date)
			{
				j++;
			}

			var current = values[i].Plus(sampling);
			var dayLast = values[j - 1];

			while (current <= dayLast)
			{
				samples.Add(current);

				current = current.Plus(sampling);
			}

			i = j;
		}

		// Collected first: the buffer must not be mutated while it is being walked.
		for (i = 0; i < samples.Count; i++)
		{
			values.Add(samples[i]);
		}
	}

	public override void Commit(bool grid)
	{
		EnsureSampled();

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
			// Snap the break window to the samples inside the view.
			start = Math.Max(0, (int)Math.Ceiling(Range.min));
			end = Math.Min(values.Count, (int)Math.Floor(Range.max) + 1);
		}
		else
		{
			SetRange(min, max);
		}

		if (!grid)
		{
			return;
		}

		var breaks = new List<double>();
		var minorBreaks = new List<double>();
		var labels = new List<(double x, string label)>();
		var titles = new List<(double x, string title)>();

		var dfirst = -1;
		var dlast = -1;

		var day = new LocalDate();

		for (int i = start; i < end; i++)
		{
			var date = values[i];

			if (date.Date != day)
			{
				if (dlast >= 0 && dlast != dfirst)
				{
					titles.Add(((dlast + dfirst) / 2.0, datePattern.Format(day)));
				}

				day = date.Date;

				dfirst = i;
			}

			if (date.Minute % 30 == 0)
			{
				breaks.Add(i);
				labels.Add((i, timePattern.Format(date.TimeOfDay)));
			}
			else if (date.Minute % 15 == 0)
			{
				minorBreaks.Add(i);
			}

			dlast = i;
		}

		if (dlast >= 0 && dlast != dfirst)
		{
			titles.Add(((dlast + dfirst) / 2.0, datePattern.Format(day)));
		}

		Breaks = breaks;
		MinorBreaks = minorBreaks;
		Labels = labels;
		Titles = titles;
	}

	public override double Map(LocalDateTime key)
	{
		EnsureSampled();

		var index = values.IndexOf(key);
		if (index < 0)
		{
			return double.NaN;
		}

		return index;
	}

	public override LocalDateTime? Unmap(double value)
	{
		EnsureSampled();

		var index = (int)Math.Round(value);

		if (index < 0 || index >= values.Count)
		{
			return null;
		}

		return values[index];
	}

	public override string? Label(LocalDateTime value) => readoutPattern.Format(value);

	// Readout formatting only; break labels keep their own day/hour logic.
	private static readonly LocalDateTimePattern readoutPattern = LocalDateTimePattern.CreateWithInvariantCulture("MMM d HH:mm");

	public override void Clear()
	{
		base.Clear();

		first = null;
		last = null;

		min = null;
		max = null;

		values.Clear();

		sampled = false;
	}
}
