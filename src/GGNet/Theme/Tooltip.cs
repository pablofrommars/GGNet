using GGNet.Elements;

namespace GGNet.Theme;

using static Units;

public sealed class Tooltip
{
	public Tooltip()
	{
		Margin = new()
		{
			Top = 5,
			Right = 10,
			Bottom = 5,
			Left = 10
		};

		Text = new()
		{
			Color = "#FFFFFF",
			Size = new() { Value = 0.75 }
		};

		Radius = new() { Value = 4, Units = px };
	}

	public Margin Margin { get; set; }

	public Text Text { get; set; }

	public string Background { get; set; } = default!;

	public double? Alpha { get; set; }

	public Size Radius { get; set; }
}