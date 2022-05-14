namespace GGNet.Theme;

using static Position;

public sealed class Theme
{
	public Theme(bool dark, Position axisY, Position legend)
	{
		Plot = new(dark);

		Panel = new(dark);

		Axis = new(dark, axisY);

		Legend = new(dark, legend);

		Strip = new(dark);

		Animation = new();

		Tooltip = new();
	}

	public string FontFamily { get; set; } = "-apple-system,BlinkMacSystemFont,\"Segoe UI\",Roboto,\"Helvetica Neue\",Arial,\"Noto Sans\",sans-serif,\"Apple Color Emoji\",\"Segoe UI Emoji\",\"Segoe UI Symbol\",\"Noto Color Emoji\"";

	public Plot Plot { get; set; }

	public Panel Panel { get; set; }

	public Axis Axis { get; set; }

	public Legend Legend { get; set; }

	public Strip Strip { get; set; }

	public Animation Animation { get; set; }

	public Tooltip Tooltip { get; set; }

	public static Theme Default(bool dark = true, Position axisY = Left, Position legend = Right)
		=> new(dark, axisY, legend);
}
