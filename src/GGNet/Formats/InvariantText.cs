namespace GGNet.Formats;

// Culture-safe stringification for arbitrary label values: IFormattable values
// (numbers, dates) format under the invariant culture so axis/strip/discrete
// labels never pick up a comma decimal from the ambient locale; everything else
// falls back to its plain ToString(). The label-side twin of the SvgFormat
// geometry choke point — same invariance guarantee, opposite side of the
// layout/paint numeric boundary.
internal static class InvariantText
{
	public static string? Of(object? value) => value switch
	{
		null => null,
		IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
		_ => value.ToString()
	};
}
