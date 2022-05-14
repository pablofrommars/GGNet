namespace GGNet.Geoms.Segment;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX> X { get; set; } = default!;

	public Func<T, TX> XEnd { get; set; } = default!;

	public Func<T, TY> Y { get; set; } = default!;

	public Func<T, TY> YEnd { get; set; } = default!;
}