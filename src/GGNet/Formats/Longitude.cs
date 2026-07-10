namespace GGNet.Formats;

internal sealed class Longitude : IFormatter<double>
{
	public static Longitude Instance => new();

	public string Format(double value) => value <= 0
		? FormattableString.Invariant($"{-value}\u00B0W")
		: FormattableString.Invariant($"{value}\u00B0E");
}
