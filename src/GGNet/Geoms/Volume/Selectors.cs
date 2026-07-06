namespace GGNet.Geoms.Volume;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; set; }

	public required Func<T, TY> Volume { get; set; }
}
