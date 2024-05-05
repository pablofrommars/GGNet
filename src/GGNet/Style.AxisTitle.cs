namespace GGNet;

using Elements;

using static Position;
using static Anchor;

public sealed partial class Style
{
  public sealed class StyleAxisTitle(Position position)
  {
    public Text X { get; set; } = new()
    {
      Anchor = End,
      FontSize = 0.75,
      Margin = new()
      {
        Top = 8,
        Right = 8,
        Bottom = 8
      }
    };

    public Text Y { get; set; }
      = position == Left
      ? new()
      {
        Anchor = End,
        FontSize = 0.75,
        Angle = -90,
        Margin = new()
        {
          Right = 12
        }
      }
      : new()
      {
        Anchor = Start,
        FontSize = 0.75,
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
