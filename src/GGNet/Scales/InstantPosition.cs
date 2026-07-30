namespace GGNet.Scales;

using Common;

using GGNet.Formats;

internal class InstantPosition : Position<Instant>
{
	private readonly IFormatter<Instant> formatter;

	public InstantPosition(Instant? start, Instant? end, IFormatter<Instant>? formatter = null)
	  : base(null, (0, 0, 0, 0))
	{
		Limits = (start, end);

		this.formatter = formatter ?? new InstantFormatter("H:mm:ss");
	}

	public override Guide Guide => Guide.None;

	public override void Commit(bool grid)
	{
		var (start, mappedStart) = (Limits.min, _min) switch
		{
			(null, null) => (Instant.FromUnixTimeSeconds(0), 0),
			(null, double min) => (Instant.FromUnixTimeMilliseconds((long)min), min),
			(Instant limit, _) => (limit, Map(limit)),
		};

		var (end, mappedEnd) = (Limits.max, _max) switch
		{
			(null, null) => (Instant.FromUnixTimeSeconds(0), 0),
			(null, double max) => (Instant.FromUnixTimeMilliseconds((long)max), max),
			(Instant limit, _) => (limit, Map(limit)),
		};

		if (CommitViewRange())
		{
			// Breaks follow the windowed range, not the trained extent.
			start = Instant.FromUnixTimeMilliseconds((long)Range.min);
			end = Instant.FromUnixTimeMilliseconds((long)Range.max);
		}
		else
		{
			SetRange(mappedStart, mappedEnd);
		}

		if (!grid)
		{
			return;
		}

		var breaks = Wilkinson.Extended(start, end);
		if (breaks is null)
		{
			return;
		}

		Breaks = breaks;

		var labels = new (double, string)[breaks.Length];

		for (var i = 0; i < labels.Length; i++)
		{
			labels[i] = (breaks[i], formatter.Format(Instant.FromUnixTimeMilliseconds((long)breaks[i])));
		}

		Labels = labels;
	}

	public override double Map(Instant key) => key.ToUnixTimeMilliseconds();

	public override Instant? Unmap(double value) => Instant.FromUnixTimeMilliseconds((long)value);

	public override string? Label(Instant value) => formatter.Format(value);
}
