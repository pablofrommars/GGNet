using GGNet.Scales;

namespace GGNet.Geoms.Hex;

internal sealed class Positions<T>
{
	// default!: wired by the geom's Init(panel, …), which AddTyped runs
	// immediately after construction — mappings need the panel's scales.
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> Y { get; set; } = default!;

	public IPositionMapping<T> Dx { get; set; } = default!;

	public IPositionMapping<T> Dy { get; set; } = default!;
}
