namespace GGNet.Facets;

public abstract class Faceting<T>
{
	public Faceting(bool freeX, bool freeY)
	{
		FreeX = freeX;
		FreeY = freeY;
	}

	public bool FreeX { get; }

	public bool FreeY { get; }

	public int N { get; protected set; }

	public int NRows { get; protected set; }

	public int NColumns { get; protected set; }

	public abstract bool Strip { get; }

	public abstract void Train(T item);

	public abstract void Set();

	public abstract (int row, int column) Map(T item);

	public abstract void Clear();

	public abstract (Facet<T> facet, bool showX, bool showY)[] Facets(Theme theme);
}