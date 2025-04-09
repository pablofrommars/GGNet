namespace GGNet;

public sealed partial class Style
{
  public sealed class StylePanel
  {
    public StylePanelSpacing Spacing { get; set; } = new()
    {
      X = 8,
      Y = 8
    };
  }
}
