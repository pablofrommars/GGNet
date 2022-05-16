namespace GGNet.Formats;

public sealed class DoubleFormatter : IFormatter<double>
{
	private readonly string format;

	public DoubleFormatter(string format) => this.format = format;

	public string Format(double value) => value.ToString(format);

	public static DoubleFormatter Instance => new("0.##");
}