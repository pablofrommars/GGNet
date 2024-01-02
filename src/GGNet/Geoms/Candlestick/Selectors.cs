namespace GGNet.Geoms.Candlestick;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; set; }

	public required Func<T, TY> Open { get; init; }

	public required Func<T, TY> High { get; init; }

	public required Func<T, TY> Low { get; init; }

	public required Func<T, TY> Close { get; init; }
}