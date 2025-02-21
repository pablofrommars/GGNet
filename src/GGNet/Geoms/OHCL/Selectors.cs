namespace GGNet.Geoms.OHCL;

internal sealed record Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; set; }

	public required Func<T, TY> Open { get; set; }

	public required Func<T, TY> High { get; set; }

	public required Func<T, TY> Low { get; set; }

	public required Func<T, TY> Close { get; set; }
}