namespace GGNet;

public static class AnchorExtensions
{
  public static string Render(this Anchor anchor) => anchor switch
  {
    Anchor.Start => "start",
    Anchor.Middle => "middle",
    Anchor.End => "end",
    _ => throw new NotImplementedException()
  };
}
