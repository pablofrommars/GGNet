using GGNet.Scales;

namespace GGNet.Geoms.Text;

internal sealed class Positions<T>
{
	public IPositionMapping<T> X { get; set; } = default!;

	public IPositionMapping<T> XEnd { get; set; } = default!;

	public IPositionMapping<T> Y { get; set; } = default!;

	public IPositionMapping<T> YEnd { get; set; } = default!;
}