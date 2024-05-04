using GGNet.Elements;

namespace GGNet.Theme;

using static LineType;

public sealed partial class Panel
{
  public Panel(bool dark)
  {
    Background = new()
    {
      Fill = dark ? "#343a40" : "#FFFFFF"
    };

    var stroke = dark ? "#464950" : "#cccccc";

    Grid = new()
    {
      Major = new()
      {
        X = new()
        {
          Stroke = stroke,
          StrokeOpacity = 1.0,
          StrokeWidth = 0.43,
          LineType = Solid
        },
        Y = new()
        {
          Stroke = stroke,
          StrokeOpacity = 1.0,
          StrokeWidth = 0.43,
          LineType = Solid
        }
      },
      Minor = new()
      {
        X = new()
        {
          Stroke = stroke,
          StrokeOpacity = 1.0,
          StrokeWidth = 0.32,
          LineType = Solid
        },
        Y = new()
        {
          Stroke = stroke,
          StrokeOpacity = 1.0,
          StrokeWidth = 0.32,
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
