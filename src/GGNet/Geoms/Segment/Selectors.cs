namespace GGNet.Geoms.Segment;

internal sealed class Selectors<T, TX, TY>
{
	public required Func<T, TX> X { get; set; }

	public required Func<T, TX> XEnd { get; set; }

	public required Func<T, TY> Y { get; set; }

	public required Func<T, TY> YEnd { get; set; }

	public Func<T, RenderFragment>? Tooltip { get; set; }
}
