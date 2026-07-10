namespace GGNet.Data;

internal sealed class Position<T>
  where T : struct
{
	public List<Scales.Position<T>> Scales { get; } = [];

	// Null until a scale is registered; Init rejects the plot before any
	// Instance() call if no factory was set and none could be defaulted.
	public Func<Scales.Position<T>>? Factory { get; set; }

	// Interaction bucket: the plot-wide view window lives on the container
	// because scale instances are disposable — faceted passes re-instance
	// them every render — while the container spans passes. Register stamps
	// it onto every new scale so re-instanced facets inherit the window.
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
