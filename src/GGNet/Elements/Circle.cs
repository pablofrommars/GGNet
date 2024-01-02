namespace GGNet.Elements;

public sealed record Circle : IElement
{
	public double Radius { get; init; }

	public required string Fill { get; init; }

	public double Alpha { get; init; }
}