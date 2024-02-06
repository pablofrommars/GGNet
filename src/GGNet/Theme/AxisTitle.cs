using GGNet.Elements;

namespace GGNet.Theme;

using static Position;
using static Anchor;

public sealed class AxisTitle
{
	public AxisTitle(bool dark, Position axisY)
	{
		var color = dark ? "#929299" : "#111827";

		X = new()
		{
			Anchor = end,
			Size = new() { Value = 0.875 },
				Color = color,
			Margin = new()
			{
				Top = 8,
				Right = 8,
				Bottom = 8
			}
		};

		if (axisY == Left)
		{
			Y = new()
			{
				Size = new() { Value = 0.875 },
				Anchor = end,
				Color = color,
				Angle = -90,
				Margin = new()
				{
					Right = 8
				}
			};
		}
		else
		{
			Y = new()
			{
				Size = new() { Value = 0.875 },
				Anchor = start,
				Color = color,
				Angle = 90,
				Margin = new()
				{
					Top = 8,
					Right = 8,
					Left = 8
				}
			};
		}
	}

	public Text X { get; set; }

	public Text Y { get; set; }
}
