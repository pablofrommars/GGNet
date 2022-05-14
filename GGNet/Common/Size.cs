namespace GGNet;

using static Units;

public sealed record Size
{
	public double Value { get; init; }

	public Units Units { get; init; } = em;

	public override string ToString() => $"{Value}{Units}";
}