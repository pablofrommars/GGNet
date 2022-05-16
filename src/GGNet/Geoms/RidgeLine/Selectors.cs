namespace GGNet.Geoms.RidgeLine;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; set; }

	public Func<T, TY>? Y { get; set; }

	public Func<T, double> Height { get; set; } = default!;
}