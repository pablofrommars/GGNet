using GGNet.Scales;

namespace GGNet.Geoms.Area;

internal sealed record Positions<T>
{
	// default!: wired by the geom's Init(panel, …), which AddTyped runs
	// immediately after construction — mappings need the panel's scales.
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> Y { get; set; } = default!;
}
