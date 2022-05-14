namespace GGNet.Elements;

public sealed record Rectangle : IElement
{
	public string Fill { get; init; } = "inhenit";

	public double Alpha { get; init; } = 1.0;

	public string Color { get; init; } = "inhenit";

	public double Width { get; init; }

	public Margin Margin { get; init; } = new();
}