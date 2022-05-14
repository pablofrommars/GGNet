namespace GGNet.Facets;

public class Facet<T>
{
	public Facet(Faceting<T> faceting, (int row, int column) coord, string? xStrip = null, string? yStrip = null)
	{
		Faceting = faceting;

		Coord = coord;

		XStrip = xStrip;
		YStrip = yStrip;
	}

	public Faceting<T> Faceting { get; }

	public (int row, int column) Coord { get; }

	public string? XStrip { get; }

	public string? YStrip { get; }

	public bool Include(T item) => Faceting.Map(item) == Coord;
}