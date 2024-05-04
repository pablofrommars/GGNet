namespace GGNet;

public static class UnitsExtensions
{
  public static string Render(this Units unit) => unit switch
  {
    Units.EM => "em",
    Units.PX => "px",
    _ => throw new NotImplementedException()
  };
}
