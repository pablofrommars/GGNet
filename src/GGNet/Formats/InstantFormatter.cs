namespace GGNet.Formats;

public sealed class InstantFormatter(string format, string timezone = "UTC") : IFormatter<Instant>
{
  private readonly ZonedDateTimePattern pattern = ZonedDateTimePattern.CreateWithInvariantCulture(format, null);
  private readonly DateTimeZone timezone = DateTimeZoneProviders.Tzdb[timezone];

  public string Format(Instant value) => pattern.Format(value.InZone(timezone));
}
