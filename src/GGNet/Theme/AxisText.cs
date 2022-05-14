namespace GGNet.Theme;

using Common;
using Elements;

using static Common.Position;
using static Common.Anchor;

public class AxisText
{
	public AxisText(bool dark, Position axisY)
	{
		X = new()
		{
			Size = new() { Value = 0.75 },
			Color = dark ? "#929299" : "#2b2b2b",
			Anchor = middle
		};

		Y = new()
		{
			Size = new() { Value = 0.75 },
			Anchor = axisY == Left ? end : start,
			Color = dark ? "#929299" : "#2b2b2b",
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