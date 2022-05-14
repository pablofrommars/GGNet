namespace GGNet.Elements;

public sealed record Circle : IElement
{
	public double Radius { get; init; }

	public string Fill { get; init; } = default!;

	public double Alpha { get; init; }
}