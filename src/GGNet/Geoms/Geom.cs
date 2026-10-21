using GGNet.Data;
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

	private Facet<T>? candidateFacet;
	private object? facetSource;
	private Facet<T>? layerFacet;
	private List<int>? facetIndices;
	private Legends? legends;

	public List<Shape> Layer { get; } = [];

	public IStatSource? StatSource { get; } = source as IStatSource;

	public virtual CoordSystem SupportedCoordSystems => CoordSystem.Cartesian | CoordSystem.Polar;

	public virtual void Init<T1>(Panel<T1, TX, TY> panel, Facet<T1>? facet)
	{
		candidateFacet = facet as Facet<T>;
		facetSource = panel.Data.Source;

		legends = panel.Data.Legends;
	}

	public abstract void Train(T item);

	public void Train()
	{
		layerFacet = candidateFacet is not null && facetSource is not null && facetSource.Equals(source)
			? candidateFacet
			: null;

		facetIndices = ReferenceEquals(facetSource, source) ? layerFacet?.Indices : null;

		for (var i = 0; i < (facetIndices?.Count ?? source.Count); i++)
		{
			var item = source[facetIndices is null ? i : facetIndices[i]];

			if (facetIndices is null && layerFacet is not null && !layerFacet.Include(item))
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
		for (var i = 0; i < (facetIndices?.Count ?? source.Count); i++)
		{
			var item = source[facetIndices is null ? i : facetIndices[i]];

			if (facetIndices is null && layerFacet is not null && !layerFacet.Include(item))
			{
				continue;
			}

			Shape(item);
		}

		Set();
	}

	public virtual void Clear() => Layer?.Clear();
}
