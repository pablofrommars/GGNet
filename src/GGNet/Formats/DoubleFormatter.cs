namespace GGNet.Formats;

/// <summary>
/// Numeric break-label formatter using a standard or custom .NET format string. Formats invariantly by default; pass a culture to localize tick labels.
/// </summary>
/// <param name="format">.NET numeric format string, e.g. <c>N1</c> or <c>0.00</c>.</param>
/// <param name="culture">Culture used to format labels; null formats invariantly.</param>
public sealed class DoubleFormatter(string format, CultureInfo? culture = null) : IFormatter<double>
{
	internal static DoubleFormatter Instance => new("N2");

	public string Format(double value) => value.ToString(format, culture ?? CultureInfo.InvariantCulture);
}
