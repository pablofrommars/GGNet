namespace GGNet.Formats;

public class Longitude : IFormatter<double>
{
	public static Longitude Instance => new();

	public string Format(double value) => value <= 0 ? $"{-value}\u00B0W" : $"{value}\u00B0E";
}