namespace GGNet;

using static Units;

public readonly record struct Size
{
  public Size() { }

  public double Value { get; init; }

  public Units Units { get; init; } = EM;

  public override string ToString() => $"{Value}{Units.Render()}";

  public static implicit operator Size(double value) => new() { Value = value };

  public static Size operator *(Size size, Units unit) => size with { Units = unit };
}
