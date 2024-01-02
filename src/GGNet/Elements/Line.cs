namespace GGNet.Elements;

public record Line : IElement
{
	public double Width { get; init; }

	public required string Fill { get; init; }

	public double Alpha { get; init; }

	public LineType LineType { get; init; }
}