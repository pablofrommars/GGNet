namespace GGNet;

using Elements;

using static Position;
using static Anchor;

public sealed partial class Style
{
  public sealed class StyleAxisText
  {
    public StyleAxisText(Position position)
    {
      X = new()
      {
        Anchor = Middle,
        FontSize = 0.75,
        Margin = new()
        {
          Top = 8
        }
      };

      Y = new()
      {
        Anchor = position == Left ? End : Start,
        FontSize = 0.75,
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
}
