namespace GGNet.Elements;

internal readonly union Element(Circle, HLine, Rectangle, VLine);

internal static class ElementExtensions
{
	public static string StopColor(this Element element) => element switch
	{
		Circle circle => circle.StopColor,
		Rectangle rectangle => rectangle.StopColor,
		HLine => "inherit",
		VLine => "inherit"
	};

	public static double StopOpacity(this Element element) => element switch
	{
		Circle circle => circle.StopOpacity,
		Rectangle rectangle => rectangle.StopOpacity,
		HLine => 1.0,
		VLine => 1.0
	};
}
