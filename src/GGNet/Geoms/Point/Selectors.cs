namespace GGNet.Geoms.Point;

internal class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; init; }

	public Func<T, TY>? Y { get; init; }

	public Func<T, string>? Tooltip { get; init; }
}