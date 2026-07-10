namespace GGNet.Formats;

/// <summary>
/// Instant break-label formatter using a NodaTime pattern, rendered in the given time zone. Formats invariantly by default; pass a culture to localize tick labels.
/// </summary>
/// <param name="format">NodaTime <c>ZonedDateTimePattern</c> text, e.g. <c>MMM dd HH:mm</c>.</param>
/// <param name="timezone">Tzdb time zone id the instant is rendered in.</param>
/// <param name="culture">Culture used to format labels; null formats invariantly.</param>
public sealed class InstantFormatter(string format, string timezone = "UTC", CultureInfo? culture = null) : IFormatter<Instant>
{
	private readonly ZonedDateTimePattern pattern = culture is null
		? ZonedDateTimePattern.CreateWithInvariantCulture(format, null)
		: ZonedDateTimePattern.CreateWithInvariantCulture(format, null).WithCulture(culture);
	private readonly DateTimeZone timezone = DateTimeZoneProviders.Tzdb[timezone];

	public string Format(Instant value) => pattern.Format(value.InZone(timezone));
}
