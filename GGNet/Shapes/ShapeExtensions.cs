namespace GGNet.Shapes;

public static class ShapeExtensions
{
	//TODO: https://docs.microsoft.com/en-us/aspnet/core/blazor/performance?view=aspnetcore-6.0#avoid-recreating-delegates-for-many-repeated-elements-or-components
	//TODO: Move to shape?
	public static Task OnClickHandler(this Shape shape, MouseEventArgs e)
		=> shape.OnClick is null ? Task.CompletedTask : shape.OnClick(e);

	//TODO: https://docs.microsoft.com/en-us/aspnet/core/blazor/performance?view=aspnetcore-6.0#avoid-recreating-delegates-for-many-repeated-elements-or-components
	public static Task OnMouseOverHandler(this Shape shape, MouseEventArgs e)
		=> shape.OnMouseOver is null ? Task.CompletedTask : shape.OnMouseOver(e);

	//TODO: https://docs.microsoft.com/en-us/aspnet/core/blazor/performance?view=aspnetcore-6.0#avoid-recreating-delegates-for-many-repeated-elements-or-components
	public static Task OnMouseOutHandler(this Shape shape, MouseEventArgs e)
		=> shape.OnMouseOut is null ? Task.CompletedTask : shape.OnMouseOut(e);

	public static string Css(this Shape shape)
	{
		var ret = shape.Classes ?? "";

		if (shape.OnClick is not null || shape.OnMouseOver is not null || shape.OnMouseOut is not null)
		{
			if (!string.IsNullOrEmpty(ret))
			{
				ret += " ";
			}

			ret += "cursor-pointer";
		}

		return ret;
	}
}