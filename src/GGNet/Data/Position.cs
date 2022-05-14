using GGNet.Common;

namespace GGNet.Data;

internal sealed class Position<T>
	where T : struct
{
	public Buffer<Scales.Position<T>> Scales { get; } = new(16, 1);

	public Func<Scales.Position<T>> Factory { get; set; } = default!;

	public void Register(Scales.Position<T> scale) => Scales.Add(scale);

	public Scales.Position<T> Instance()
	{
		var instance = Factory();

		Register(instance);

		return instance;
	}
}