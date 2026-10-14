namespace GGNet.Formats;

internal static class InvariantText
{
	public static string? Of(object? value) => value switch
	{
		null => null,
		IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString()
	};
}
