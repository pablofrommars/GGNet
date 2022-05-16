using GGNet.Scales;

namespace GGNet.Geoms.Violin;

internal sealed class Aesthetics<T>
{
	public IAestheticMapping<T, string>? Fill { get; set; }
}