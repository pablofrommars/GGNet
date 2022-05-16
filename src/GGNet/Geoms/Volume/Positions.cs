using GGNet.Scales;

namespace GGNet.Geoms.Volume;

internal sealed class Positions<T>
{
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> Volume { get; set; } = default!;
}