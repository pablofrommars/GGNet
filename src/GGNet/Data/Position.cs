
namespace GGNet.Data;

internal sealed class Position<T>
  where T : struct
{
	public List<Scales.Position<T>> Scales { get; } = [];

	// Null until a scale is registered; Init rejects the plot before any
	// Instance() call if no factory was set and none could be defaulted.
	public Func<Scales.Position<T>>? Factory { get; set; }

	public void Register(Scales.Position<T> scale) => Scales.Add(scale);

	public Scales.Position<T> Instance()
	{
		var instance = Factory!();

		Register(instance);

		return instance;
	}
}
