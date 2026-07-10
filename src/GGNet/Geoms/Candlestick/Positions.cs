using GGNet.Scales;

namespace GGNet.Geoms.Candlestick;

internal sealed class Positions<T>
{
	// default!: wired by the geom's Init(panel, …), which AddTyped runs
	// immediately after construction — mappings need the panel's scales.
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> Open { get; set; } = default!;

	public IPositionMapping<T> High { get; set; } = default!;

	public IPositionMapping<T> Low { get; set; } = default!;

	public IPositionMapping<T> Close { get; set; } = default!;
}
