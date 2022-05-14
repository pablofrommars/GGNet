namespace GGNet.NaturalEarth;

[MessagePackObject]
public record City
{
	[Key(0)]
	public string Name { get; init; } = default!;

	[Key(1)]
	public Point Point { get; init; } = default!;
}

[MessagePackObject]
public record Country
{
	[Key(0)]
	public string A2 { get; init; } = default!;

	[Key(1)]
	public string A3 { get; init; } = default!;

	[Key(2)]
	public string Name { get; init; } = default!;

	[Key(3)]
	public string Continent { get; init; } = default!;

	[Key(4)]
	public Polygon[] Polygons { get; init; } = default!;

	[Key(5)]
	public City Capital { get; init; } = default!;

	[Key(6)]
	public Point Centroid { get; init; } = default!;

	public sealed override string ToString() => $"{A2} - {Name}";
}

[MessagePackObject]
public record Lake
{
	[Key(0)]
	public string Name { get; init; } = default!;

	[Key(1)]
	public Polygon[] Polygons { get; init; } = default!;
}