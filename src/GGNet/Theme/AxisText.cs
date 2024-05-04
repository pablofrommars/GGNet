namespace GGNet.Theme;

using Elements;

using static Position;
using static Anchor;

public sealed class AxisText
{
	public AxisText(bool dark, Position axisY)
	{
		X = new()
		{
			Anchor = Middle,
			FontSize = 0.75,
			Color = dark ? "#929299" : "#374151",
      Margin = new()
      {
        Top = 8
      }
    };

		Y = new()
		{
			Anchor = axisY == Left ? End : Start,
      FontSize = 0.75,
			Color = dark ? "#929299" : "#374151",
			Margin = new()
			{
				Right = 4,
				Left = 4
			}
		};
	}

	public Text X { get; set; }

	public Text Y { get; set; }
}
