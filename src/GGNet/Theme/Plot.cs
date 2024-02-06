using GGNet.Elements;

namespace GGNet.Theme;

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
			Size = new() { Value = 1.25 },
			Weight = "bold",
			Color = dark ? "#FFFFFF" : "#111827",
			Margin = new() { Bottom = 16 }
		};

		SubTitle = new()
		{
			Size = new() { Value = 1.125 },
			Color = dark ? "#929299" : "#374151",
			Margin = new() { Bottom = 16 }
		};

		Caption = new()
		{
			Size = new() { Value = 0.875 },
			Anchor = end,
			Style = "italic",
			Color = dark ? "#929299" : "#6b7280",
			Margin = new()
			{
				Top = 8,
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
