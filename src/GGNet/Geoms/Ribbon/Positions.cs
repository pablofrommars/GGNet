using GGNet.Scales;

namespace GGNet.Geoms.Ribbon;

internal sealed class Positions<T>
{
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> YMin { get; set; } = default!;

	public IPositionMapping<T> YMax { get; set; } = default!;
}