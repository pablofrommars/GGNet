namespace GGNet.Geoms.RidgeLine;

internal sealed record Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; init; }

	public Func<T, TY>? Y { get; init; }

	public required Func<T, double> Height { get; init; }
}