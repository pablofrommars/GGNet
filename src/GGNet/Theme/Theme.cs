namespace GGNet.Theme;

using static Position;

public sealed class Theme(bool dark, Position axisY, Position legend)
{
  public string FontFamily { get; set; } = "Inter var";

  public Plot Plot { get; set; } = new(dark);

  public Panel Panel { get; set; } = new(dark);

  public Axis Axis { get; set; } = new(dark, axisY);

  public Legend Legend { get; set; } = new(dark, legend);

  public Strip Strip { get; set; } = new(dark);

  public Tooltip Tooltip { get; set; } = new();

  public static Theme Default(bool dark = true, Position axisY = Left, Position legend = Right)
  => new(dark, axisY, legend);
}
