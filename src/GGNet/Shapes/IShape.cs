namespace GGNet.Shapes;

public interface IShape
{
  string? Classes { get; }

  Func<MouseEventArgs, Task>? OnClick { get; }

  Task OnClickHandler(MouseEventArgs e)
    => OnClick is null ? Task.CompletedTask : OnClick(e);

  Func<MouseEventArgs, Task>? OnMouseOver { get; }

  Task OnMouseOverHandler(MouseEventArgs e)
    => OnMouseOver is null ? Task.CompletedTask : OnMouseOver(e);

  Func<MouseEventArgs, Task>? OnMouseOut { get; }

  Task OnMouseOutHandler(MouseEventArgs e)
    => OnMouseOut is null ? Task.CompletedTask : OnMouseOut(e);

  string Css => (Classes, OnClick, OnMouseOver, OnMouseOut) switch
  {
    (null, null, null, null) => string.Empty,
    (_, null, null, null) => Classes,
    (null, _, _, _) => "cursor-pointer",
    _ => $"{Classes} cursor-pointer"
  };
}
