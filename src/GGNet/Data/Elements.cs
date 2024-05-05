using GGNet.Buffers;
using GGNet.Elements;

using static System.Math;

namespace GGNet.Data;

internal sealed class Elements(double size) : Buffer<Dimension<IElement>>(4, 1)
{
	private readonly double size = size;

    public double Width { get; set; }

	public double Height { get; set; }

	public Dimension<IElement> Add(IElement element)
	{
		var dim = new Dimension<IElement>
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
