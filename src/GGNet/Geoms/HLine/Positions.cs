using GGNet.Scales;

namespace GGNet.Geoms.HLine;

internal sealed class Positions<T>
{
	public IPositionMapping<T> Y { get; set; } = default!;
}