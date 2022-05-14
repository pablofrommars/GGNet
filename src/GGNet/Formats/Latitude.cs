namespace GGNet.Formats;

public class Latitude : IFormatter<double>
{
	public static Latitude Instance => new();

	public string Format(double value) => value >= 0 ? $"{value}\u00B0N" : $"{-value}\u00B0S";
}