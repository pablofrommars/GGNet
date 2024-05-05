namespace GGNet;

public sealed partial class Style
{
  public sealed class StyleAxis(Position position)
  {
    public Position Y { get; set; } = position;

    public StyleAxisText Text { get; set; } = new(position);

    public StyleAxisTitle Title { get; set; } = new(position);
  }
}
