namespace GGNet.Theme;

using Elements;

public sealed class StripText
{
	public StripText(bool dark)
	{
		var color = dark ? "#929299" : "#212529";

		X = new()
		{
			Size = new() { Value = 0.75 },
			Color = color,
			Weight = "bold",
			Margin = new()
			{
				Left = 4,
				Top = 4,
				Bottom = 4
			}
		};

		Y = new()
		{
			Size = new() { Value = 0.75 },
			Color = color,
			Weight = "bold",
			Angle = 90,
			Margin = new()
			{
				Left = 4,
				Top = 4,
				Right = 4
			}
		};
	}

	public Text X { get; set; }

	public Text Y { get; set; }
}