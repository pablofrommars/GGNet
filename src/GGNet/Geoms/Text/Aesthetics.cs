using GGNet.Scales;

namespace GGNet.Geoms.Text;

internal sealed class Aesthetics<T>
{
	public IAestheticMapping<T, string>? Color { get; set; }
}