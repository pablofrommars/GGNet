namespace GGNet.Formats;

public sealed class DoubleFormatter(string format) : IFormatter<double>
{
  private readonly string format = format;

  public string Format(double value) => value.ToString(format);

  public static DoubleFormatter Instance => new("N2");
}
