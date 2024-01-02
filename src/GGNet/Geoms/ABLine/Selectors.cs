namespace GGNet.Geoms.ABLine;

internal sealed record Selectors<T>
{
	public required Func<T, double> A { get; init; }

	public required Func<T, double> B { get; init; }

	public Func<T, string>? Label { get; init; }
}