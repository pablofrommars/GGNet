namespace GGNet.Geoms.Point;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; init; }

	public Func<T, TY>? Y { get; init; }

	public Func<T, RenderFragment>? Tooltip { get; init; }
}
