namespace GGNet.Formats;

/// <summary>
/// General-purpose break-label formatter. Formats invariantly by default; pass a culture to localize tick labels.
/// </summary>
/// <param name="culture">Culture used to format labels; null formats invariantly.</param>
public sealed class Standard<T>(CultureInfo? culture = null) : IFormatter<T>
  where T : notnull
{
	internal static Standard<T> Instance => new();

	public string Format(T value) => value is IFormattable formattable
		? formattable.ToString(null, culture ?? CultureInfo.InvariantCulture)
		: value.ToString()!;
}
