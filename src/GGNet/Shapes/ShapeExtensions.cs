namespace GGNet.Shapes;

public static class ShapeExtensions
{
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