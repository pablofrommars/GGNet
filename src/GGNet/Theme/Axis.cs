using GGNet.Common;

namespace GGNet.Theme;

using static Position;

public sealed partial class Axis
{
	public Axis(bool dark, Position axisY)
	{
		axisY = axisY == Right ? Right : Left;

		Y = axisY;
		Text = new(dark, axisY);
		Title = new(dark, axisY);
	}

	public Position Y { get; set; }

	public AxisText Text { get; set; }

	public AxisTitle Title { get; set; }
}