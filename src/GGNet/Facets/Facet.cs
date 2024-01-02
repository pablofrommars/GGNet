namespace GGNet.Facets;

public sealed class Facet<T>(Faceting<T> faceting, (int row, int column) coord, string? xStrip = null, string? yStrip = null)
{
    public Faceting<T> Faceting { get; } = faceting;

    public (int row, int column) Coord { get; } = coord;

    public string? XStrip { get; } = xStrip;

    public string? YStrip { get; } = yStrip;

    public bool Include(T item) => Faceting.Map(item) == Coord;
}