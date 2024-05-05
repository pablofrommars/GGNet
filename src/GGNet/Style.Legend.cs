namespace GGNet;

using Elements;

using static Position;
using static Direction;
using static Anchor;

public sealed partial class Style
{
  public sealed class StyleLegend(Position position)
  {
    public Position Position { get; set; } = position;

    public Direction Direction { get; set; } = position switch
    {
      Right or Left => Vertical,
      Top or Bottom => Horizontal,
      _ => throw new NotImplementedException()
    };

    public Text Title { get; set; } = new()
    {
      Anchor = Start,
      FontSize = 0.75,
      Margin = new()
      {
        Top = 4,
        Right = position switch
        {
          Right or Left => 0,
          Top or Bottom => 16,
          _ => throw new NotImplementedException()
        },
        Bottom = 4
      }
    };

    public Text Labels { get; set; } = new()
    {
      Anchor = Start,
      FontSize = 0.75,
      Margin = new()
      {
        Right = position switch
        {
          Right or Left => 0,
          Top or Bottom => 8,
          _ => throw new NotImplementedException()
        },
        Bottom = 4,
        Left = 4
      }
    };

    public Margin Margin { get; set; } = new()
    {
      Right = position switch
      {
        Right or Left => 4,
        Top or Bottom => 0,
        _ => throw new NotImplementedException()
      },
      Left = position switch
      {
        Right or Left => 4,
        Top or Bottom => 0,
        _ => throw new NotImplementedException()
      }
    };
  }
}
