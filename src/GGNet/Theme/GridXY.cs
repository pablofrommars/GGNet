namespace GGNet.Theme;

using Elements;

public sealed record GridXY
{
	public required Line X { get; init; }

	public required Line Y { get; init; } 
}