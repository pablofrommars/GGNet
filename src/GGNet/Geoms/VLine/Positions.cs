using GGNet.Scales;

namespace GGNet.Geoms.VLine;

internal sealed class Positions<T>
{
	public IPositionMapping<T> X { get; set; } = default!;
}