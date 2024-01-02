namespace GGNet.Facets;

using Theme;

public abstract class Faceting<T>(bool freeX, bool freeY)
{
    public bool FreeX { get; } = freeX;

    public bool FreeY { get; } = freeY;

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