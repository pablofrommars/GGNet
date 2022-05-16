namespace GGNet.Geoms.Candlestick;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; set; }

	public Func<T, TY> Open { get; set; } = default!;

	public Func<T, TY> High { get; set; } = default!;

	public Func<T, TY> Low { get; set; } = default!;

	public Func<T, TY> Close { get; set; } = default!;
}