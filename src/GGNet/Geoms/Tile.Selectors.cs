namespace GGNet.Geoms.Tile;

internal sealed class Selectors<T, TX, TY>
{
	public required Func<T, TX> X { get; set; }

	public required Func<T, TY> Y { get; set; }

	public required Func<T, double> Width { get; set; }

	public required Func<T, double> Height { get; set; }
}
