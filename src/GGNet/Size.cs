namespace GGNet;

using static Units;

public readonly record struct Size
{
  public Size() { }

  public double Value { get; init; }

	public Units Units { get; init; } = em;

	public override string ToString() => $"{Value}{Units}";
}
