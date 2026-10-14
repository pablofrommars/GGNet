namespace GGNet.Data;

internal sealed class Position<T>
  where T : struct
{
	public List<Scales.Position<T>> Scales { get; } = [];

	public Func<Scales.Position<T>>? Factory { get; set; }

	public (double min, double max)? View { get; private set; }

	public void SetView((double min, double max)? view)
	{
		View = view;

		for (var i = 0; i < Scales.Count; i++)
		{
			Scales[i].ViewRange = view;
		}
	}

	public void Register(Scales.Position<T> scale)
	{
		scale.ViewRange = View;

		Scales.Add(scale);
	}

	public Scales.Position<T> Instance()
	{
		var instance = Factory!();

		Register(instance);

		return instance;
	}
}
