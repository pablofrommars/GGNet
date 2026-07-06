namespace GGNet.Elements;

// The closed set of legend swatch elements. Line is deliberately not a case:
// line legends draw HLine swatches, and closing the union to what the legend
// renderer actually handles is what makes its dispatch exhaustive.
internal readonly union Element(Circle, HLine, Rectangle, VLine);

// Colorbar gradient stops: Circle and Rectangle contribute their fill; the
// line elements keep the old IElement defaults (inherit / opaque).
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
