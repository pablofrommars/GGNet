using GGNet.Data;
using GGNet.Exceptions;
using GGNet.Facets;
using GGNet.Scales;
using GGNet.Shapes;

namespace GGNet.Geoms;

internal abstract class Geom<T, TX, TY>(IReadOnlyList<T> source, (bool x, bool y)? scale) : IGeom
  where TX : struct
  where TY : struct
{
	protected readonly IReadOnlyList<T> source = source;
	protected readonly (bool x, bool y) scale = scale ?? (true, true);

	private Facet<T>? facet;
	private Legends? legends;

	public List<Shape> Layer { get; } = [];

	public virtual CoordSystem SupportedCoordSystems => CoordSystem.Cartesian | CoordSystem.Polar;

	// Geoms share the panel's axis types by construction, so position mappings are
	// direct — no runtime type tests.
	public virtual void Init<T1>(Panel<T1, TX, TY> panel, Facet<T1>? facet)
	{
		if (facet is not null && panel.Data.Source is not null && panel.Data.Source.Equals(source))
		{
			this.facet = (facet as Facet<T>)!;
		}

		legends = panel.Data.Legends;
	}

	public abstract void Train(T item);

	public void Train()
	{
		for (var i = 0; i < source.Count; i++)
		{
			var item = source[i];

			if (facet is not null && !facet.Include(item))
			{
				continue;
			}

			Train(item);
		}
	}

	protected void Legend<TV>(IAestheticMapping<T, TV>? aes, Func<TV, Elements.Element> element)
	{
		if (legends is null)
		{
			return;
		}

		if (aes is null || !aes.Guide)
		{
			return;
		}

		var legend = legends.GetOrAdd(aes);

		foreach (var (value, label) in aes.Labels)
		{
			legend.Add(label, element(value));
		}
	}

	protected void Legend<TV>(IAestheticMapping<T, TV>? aes, Func<TV, Elements.Element[]> elements)
	{
		if (legends is null)
		{
			return;
		}

		if (aes is null || !aes.Guide)
		{
			return;
		}

		var legend = legends.GetOrAdd(aes);

		foreach (var (value, label) in aes.Labels)
		{
			var array = elements(value);

			for (var j = 0; j < array.Length; j++)
			{
				legend.Add(label, array[j]);
			}
		}
	}

	public virtual void Legend()
	{
	}

	protected abstract void Shape(T item);

	protected virtual void Set()
	{
	}

	public void Shape()
	{
		for (var i = 0; i < source.Count; i++)
		{
			var item = source[i];

			if (facet is not null && !facet.Include(item))
			{
				continue;
			}

			Shape(item);
		}

		Set();
	}

	public virtual void Clear() => Layer?.Clear();
}
