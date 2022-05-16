namespace GGNet.Geoms.Violin;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; set; }

	public Func<T, TY>? Y { get; set; }

	public Func<T, double> Width { get; set; } = default!;
}