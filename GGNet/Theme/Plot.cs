namespace GGNet.Theme;

using Elements;

using static Anchor;

public sealed class Plot
{
	public Plot(bool dark)
	{
		Background = new()
		{
			Fill = dark ? "#343a40" : "#FFFFFF"
		};

		Title = new()
		{
			Size = new() { Value = 1.125 },
			Weight = "bold",
			Color = dark ? "#FFFFFF" : "#212529",
			Margin = new() { Bottom = 8 }
		};

		SubTitle = new()
		{
			Size = new() { Value = 0.8125 },
			Color = dark ? "#929299" : "#212529",
			Margin = new() { Bottom = 8 }
		};

		Caption = new()
		{
			Size = new() { Value = 0.75 },
			Anchor = end,
			Style = "italic",
			Color = dark ? "#929299" : "#212529",
			Margin = new()
			{
				Top = 4,
				Right = 4,
				Bottom = 4,
				Left = 4
			}
		};
	}

	public Rectangle Background { get; set; }

	public Text Title { get; set; }

	public Text SubTitle { get; set; }

	public Text Caption { get; set; }
}