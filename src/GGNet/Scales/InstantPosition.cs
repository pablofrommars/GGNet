namespace GGNet.Scales;

using Common;

public class InstantPosition : Position<Instant>
{
  private readonly ZonedDateTimePattern pattern;
  private readonly DateTimeZone timezone;

  public InstantPosition(Instant start, Instant end, string format = "H:mm:ss", string timezone = "UTC")
    : base(null, (0, 0, 0, 0))
  {
    Limits = (start, end);

    pattern = ZonedDateTimePattern.CreateWithInvariantCulture(format, null);

    this.timezone = DateTimeZoneProviders.Tzdb[timezone];
  }

  public override Guide Guide => Guide.None;

  public override void Set(bool grid)
  {
    var start = Limits.min ?? throw new NullReferenceException();
    var end = Limits.max ?? throw new NullReferenceException();

    SetRange(Map(start), Map(end));

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
