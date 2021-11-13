namespace GGNet.Elements;

public class Margin
{
	public Margin(double top = 0, double right = 0, double bottom = 0, double left = 0, Units units = Units.px)
	{
		Top = top;
		Right = right;
		Bottom = bottom;
		Left = left;
		Units = units;
	}

	public double Top { get; set; }

	public double Right { get; set; }

	public double Bottom { get; set; }

	public double Left { get; set; }

	public Units Units { get; set; }
}