namespace GGNet.Shapes;

public record class Shape
{
	public string? Classes { get; internal set; }

	public Func<MouseEventArgs, Task>? OnClick { get; internal set; }

	public Task OnClickHandler(MouseEventArgs e)
		=> OnClick is null ? Task.CompletedTask : OnClick(e);

	public Func<MouseEventArgs, Task>? OnMouseOver { get; internal set; }

	public Task OnMouseOverHandler(MouseEventArgs e)
		=> OnMouseOver is null ? Task.CompletedTask : OnMouseOver(e);

	public Func<MouseEventArgs, Task>? OnMouseOut { get; internal set; }

	public Task OnMouseOutHandler(MouseEventArgs e)
		=> OnMouseOut is null ? Task.CompletedTask : OnMouseOut(e);
}