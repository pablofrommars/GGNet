using GGNet.Scales;

namespace GGNet.Geoms.Rectangle;

internal sealed class Positions<T>
{
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> Y { get; set; } = default!;
}
