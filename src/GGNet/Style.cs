namespace GGNet;

using static Position;

public sealed partial class Style(Position axisY, Position legend)
{
  public string FontFamily { get; set; } = "Inter var";

  public StylePlot Plot { get; set; } = new();

  public StylePanel Panel { get; set; } = new();

  public StyleAxis Axis { get; set; } = new(axisY);

  public StyleLegend Legend { get; set; } = new(legend);

  public StyleStrip Strip { get; set; } = new();

  public StyleTooltip Tooltip { get; set; } = new();

  public static Style Default(Position axisY = Left, Position legend = Right)
    => new(axisY, legend);
}
