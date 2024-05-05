namespace GGNet;

using Elements;

public sealed partial class Style
{
  public sealed class StylePlot
  {
    public Text Title { get; set; } = new()
    {
      FontSize = 1.25,
      Margin = new() { Bottom = 16 }
    };

    public Text SubTitle { get; set; } = new()
    {
      FontSize = 1.125,
      Margin = new() { Bottom = 16 }
    };

    public Text Caption { get; set; } = new()
    {
      FontSize = 0.875,
      Margin = new()
      {
        Top = 8,
        Right = 4,
        Bottom = 4,
        Left = 4
      }
    };
  }
}
