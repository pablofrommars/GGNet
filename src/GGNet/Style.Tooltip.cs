namespace GGNet;

using Elements;

using static Units;

public sealed partial class Style
{
  public sealed class StyleTooltip
  {
    public Margin Margin { get; set; } = new()
    {
      Top = 5,
      Right = 10,
      Bottom = 5,
      Left = 10
    };

    public Text Text { get; set; } = new()
    {
      FontSize = 0.75
    };

    public string Background { get; set; } = default!;

    public double? Opacity { get; set; }

    public Size Radius { get; set; } = (Size)4.0 * PX;
  }
}
