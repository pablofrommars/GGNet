using GGNet.Scales;

namespace GGNet.Geoms.Boxplot;

internal sealed record Positions<T>
{
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> Y { get; set; } = default!;
}
