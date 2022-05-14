using GGNet.Common;
using GGNet.Elements;

using static System.Math;

namespace GGNet.Data;

internal sealed class Elements : Buffer<Dimension<IElement>>
{
	private readonly double size;

	public Elements(double size) : base(4, 1)
	{
		this.size = size;
	}

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