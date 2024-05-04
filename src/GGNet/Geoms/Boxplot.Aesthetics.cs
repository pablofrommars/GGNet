using GGNet.Scales;

namespace GGNet.Geoms.Boxplot;

internal sealed class Aesthetics<T>
{
	public IAestheticMapping<T, string>? Fill { get; set; }
}
