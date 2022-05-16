using GGNet.Scales;

namespace GGNet.Geoms.Hex;

internal sealed class Positions<T>
{
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> Y { get; set; } = default!;

	public IPositionMapping<T> Dx { get; set; } = default!;

	public IPositionMapping<T> Dy { get; set; } = default!;
}