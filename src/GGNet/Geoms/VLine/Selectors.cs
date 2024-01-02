namespace GGNet.Geoms.VLine;

internal sealed record Selectors<T, TX>
{
	public required Func<T, TX> X { get; init; }

	public required Func<T, string> Label { get; init; }
}