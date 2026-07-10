namespace GGNet.Formats;

internal sealed class Latitude : IFormatter<double>
{
	public static Latitude Instance => new();

	public string Format(double value) => value >= 0
		? FormattableString.Invariant($"{value}\u00B0N")
		: FormattableString.Invariant($"{-value}\u00B0S");
}
