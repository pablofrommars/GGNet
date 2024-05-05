namespace GGNet;

using Elements;

public sealed partial class Style
{
  public sealed class StyleStripText
  {
    public Text X { get; set; } = new()
    {
      FontSize = 0.75,
      Margin = new()
      {
        Left = 4,
        Top = 4,
        Bottom = 4
      }
    };

    public Text Y { get; set; } = new()
    {
      FontSize = 0.75,
      Angle = 90,
      Margin = new()
      {
        Left = 4,
        Top = 4,
        Right = 4
      }
    };
  }
}
