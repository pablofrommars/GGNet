namespace GGNet.Theme;

using Elements;

public sealed class StripText
{
	public StripText(bool dark)
	{
		var color = dark ? "#929299" : "#212529";

		X = new()
		{
      FontSize = 0.75,
			FontWeight = "bold",
			Color = color,
			Margin = new()
			{
				Left = 4,
				Top = 4,
				Bottom = 4
			}
		};

		Y = new()
		{
      FontSize = 0.75,
			FontWeight = "bold",
			Color = color,
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
