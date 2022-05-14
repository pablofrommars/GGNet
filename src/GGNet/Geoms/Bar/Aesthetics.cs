using GGNet.Scales;

namespace GGNet.Geoms.Bar;

internal sealed class Aesthetics<T>
{
	public IAestheticMapping<T, string>? Fill { get; set; }
}