namespace GGNet.Elements;

public readonly record struct Margin
{
  public Margin() { }

  public double Top { get; init; }

	public double Right { get; init; }

	public double Bottom { get; init; }

	public double Left { get; init; }

	public Units Units { get; init; } = Units.px;
}
