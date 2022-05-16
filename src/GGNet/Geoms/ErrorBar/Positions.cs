using GGNet.Scales;

namespace GGNet.Geoms.ErrorBar;

internal sealed class Positions<T>
{
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> Y { get; set; } = default!;

	public IPositionMapping<T> YMin { get; set; } = default!;

	public IPositionMapping<T> YMax { get; set; } = default!;
}