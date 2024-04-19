namespace GGNet.Geoms.Rectangle;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX> X { get; set; } = default!;

	public Func<T, TY> Y { get; set; } = default!;

	public Func<T, double> Width { get; set; } = default!;

	public Func<T, double> Height { get; set; } = default!;
}
