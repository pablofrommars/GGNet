namespace GGNet.Components;

internal static class SvgFormat
{
	public static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

	public static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

	public static string Attr(FormattableString value) => FormattableString.Invariant(value);
}
