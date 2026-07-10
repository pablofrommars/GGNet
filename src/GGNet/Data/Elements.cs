using GGNet.Elements;

using static System.Math;

namespace GGNet.Data;

internal sealed class Elements(double size) : List<Dimension<Element>>
{
	private readonly double size = size;

	public double Width { get; set; }

	public double Height { get; set; }

	public Dimension<Element> Add(Element element)
	{
		var dim = new Dimension<Element>
		{
			Value = element,
			Width = size,
			Height = size
		};

		if (element is Circle c)
		{
			var diam = 2 * c.Radius;

			dim.Width = Max(dim.Width, diam);
			dim.Height = Max(dim.Height, diam);
		}

		Width = Max(Width, dim.Width);
		Height = Max(Height, dim.Height);

		Add(dim);

		return dim;
	}
}
