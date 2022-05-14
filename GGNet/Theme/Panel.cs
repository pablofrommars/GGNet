namespace GGNet.Theme;

using Elements;

using static LineType;

public sealed partial class Panel
{
	public Panel(bool dark)
	{
		Background = new()
		{
			Fill = dark ? "#343a40" : "#FFFFFF"
		};

		var color = dark ? "#464950" : "#cccccc";

		Grid = new()
		{
			Major = new()
			{
				X = new()
				{
					Width = 0.43,
					Fill = color,
					Alpha = 1.0,
					LineType = Solid
				},
				Y = new()
				{
					Width = 0.43,
					Fill = color,
					Alpha = 1.0,
					LineType = Solid
				}
			},
			Minor = new()
			{
				X = new()
				{
					Width = 0.32,
					Fill = color,
					Alpha = 1.0,
					LineType = Solid
				},
				Y = new()
				{
					Width = 0.32,
					Fill = color,
					Alpha = 1.0,
					LineType = Solid
				}
			},
		};

		Spacing = new()
		{
			X = 8,
			Y = 8
		};
	}

	public Rectangle Background { get; set; }

	public PaneGrid Grid { get; set; }

	public PanelSpacing Spacing { get; set; }
}