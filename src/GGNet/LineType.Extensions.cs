namespace GGNet;

public static class LineTypeExtensions
{
  public static string Render(this LineType type) => type switch
  {
    LineType.Solid => "1 0",
    LineType.Dashed => "10 10",
    LineType.Dotted => "2 5",
    LineType.DotDash => "2 5 10 5",
    LineType.LongDash => "15 5",
    LineType.TowDash => "5 5 15 5",
    _ => throw new NotImplementedException()
  };
}
