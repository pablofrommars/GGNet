using GGNet.Scales;

namespace GGNet.Data;

internal sealed class Aesthetics<T>
{
	public List<IScale> Scales { get; } = [];

	public IAestheticMapping<T, string>? Color { get; set; }

	public IAestheticMapping<T, string>? Fill { get; set; }

	public IAestheticMapping<T, double>? Size { get; set; }

	public IAestheticMapping<T, LineType>? LineType { get; set; }
}
