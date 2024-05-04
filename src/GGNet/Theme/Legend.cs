namespace GGNet.Theme;

using Elements;

using static Position;
using static Direction;
using static Anchor;

public sealed class Legend
{
  public Legend(bool dark, Position legend)
  {
    if (legend == Right)
    {
      Position = Right;
      Direction = Vertical;
    }
    else if (legend == Left)
    {
      Position = Left;
      Direction = Vertical;
    }
    else if (legend == Top)
    {
      Position = Top;
      Direction = Horizontal;
    }
    else
    {
      Position = Bottom;
      Direction = Horizontal;
    }

    var color = dark ? "#929299" : "#212529";

    Title = new()
    {
      Anchor = Start,
      FontSize = 0.75,
      FontWeight = "bold",
      Color = color,
      Margin = new()
      {
        Top = 4,
        Right = Direction == Horizontal ? 16 : 0,
        Bottom = 4
      }
    };

    Labels = new()
    {
      Anchor = Start,
      FontSize = 0.75,
      Color = color,
      Margin = new()
      {
        Right = Direction == Horizontal ? 8 : 0,
        Bottom = 4,
        Left = 4
      }
    };

    Margin = new()
    {
      Right = Direction == Vertical ? 4 : 0,
      Left = Direction == Vertical ? 4 : 0
    };
  }

  public Position Position { get; set; }

  public Direction Direction { get; set; }

  public Text Title { get; set; }

  public Text Labels { get; set; }

  public Margin Margin { get; set; }
}
