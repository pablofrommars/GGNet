namespace GGNet.Scales;

using Common;

public class InstantPosition : Position<Instant>
{
  private readonly ZonedDateTimePattern pattern;
  private readonly DateTimeZone timezone;

  public InstantPosition(Instant? start, Instant? end, string format = "H:mm:ss", string timezone = "UTC")
    : base(null, (0, 0, 0, 0))
  {
    Limits = (start, end);

    pattern = ZonedDateTimePattern.CreateWithInvariantCulture(format, null);

    this.timezone = DateTimeZoneProviders.Tzdb[timezone];
  }

  public override Guide Guide => Guide.None;

  public override void Set(bool grid)
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

    SetRange(mappedStart, mappedEnd);

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
      var zoned = Instant.FromUnixTimeMilliseconds((long)breaks[i]).InZone(timezone);

      labels[i] = (breaks[i], pattern.Format(zoned));
    }

    Labels = labels;
  }

  public override double Map(Instant key) => key.ToUnixTimeMilliseconds();
}
