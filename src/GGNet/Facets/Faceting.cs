namespace GGNet.Facets;

internal abstract class Faceting<T>(bool freeX, bool freeY)
{
	public bool FreeX { get; } = freeX;

	public bool FreeY { get; } = freeY;

	public int N { get; protected set; }

	public int NRows { get; protected set; }

	public int NColumns { get; protected set; }

	public abstract bool Strip { get; }

	public abstract void Train(T item);

	public abstract void Commit();

	public abstract (int row, int column) Map(T item);

	public void Index(IReadOnlyList<T>? source, (Facet<T> facet, bool showX, bool showY)[] facets)
	{
		if (source is null || NRows <= 0 || NColumns <= 0 || (long)NRows * NColumns < facets.Length)
		{
			return;
		}

		Dictionary<(int row, int column), List<int>> buckets = [with(facets.Length)];

		foreach (var (facet, _, _) in facets)
		{
			var indices = new List<int>();
			facet.Indices = indices;
			buckets.Add(facet.Coord, indices);
		}

		for (var i = 0; i < source.Count; i++)
		{
			if (buckets.TryGetValue(Map(source[i]), out var indices))
			{
				indices.Add(i);
			}
		}
	}

	public abstract void Clear();

	public abstract (Facet<T> facet, bool showX, bool showY)[] Facets(Style style);
}
