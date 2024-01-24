namespace GGNet.Elements;

public record Text
{
	public Size Size { get; init; } = new() { Value = 1 };

	public Anchor Anchor { get; init; } = Anchor.start;

	public string Weight { get; init; } = "normal";

	public string Style { get; init; } = "normal";

	public string Color { get; init; } = "#929299";

	public string Fill { get; init; } = "#FFFFFF";

	public double Alpha { get; init; } = 1.0;

	public double Angle { get; init; }

	public Margin Margin { get; init; } = new();
}
