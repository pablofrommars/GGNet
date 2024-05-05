using GGNet.Buffers;
using GGNet.Scales;

namespace GGNet.Data;

internal sealed class Aesthetics<T>
{
	public Buffer<IScale> Scales { get; } = new(16, 1);

	public IAestheticMapping<T, string>? Color { get; set; }

	public IAestheticMapping<T, string>? Fill { get; set; }

	public IAestheticMapping<T, double>? Size { get; set; }

	public IAestheticMapping<T, LineType>? LineType { get; set; }
}
