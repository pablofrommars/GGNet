namespace GGNet.Elements;

public readonly record struct Text
{
  public Text() { }

	public Anchor Anchor { get; init; } = Anchor.Start;

  public string FontFamily { get; init; } = "Inter var";

  public Size FontSize { get; init; } = 1.0;

	public string FontWeight { get; init; } = "normal";

	public string FontStyle { get; init; } = "normal";

	public string Color { get; init; } = "#929299";

	public double Opacity { get; init; } = 1.0;

	public double Angle { get; init; }

	public Margin Margin { get; init; } = new();
}
