using GGNet.Buffers;
using GGNet.Elements;
using GGNet.Scales;

using static System.Math;

namespace GGNet.Data;

internal sealed class Legend
{
	public Legend(Style style, IAestheticMapping aes)
	{
		Aes = aes;

		if (!string.IsNullOrEmpty(aes.Name))
		{
			Title = new()
			{
				Value = aes.Name,
				Width = aes.Name.Width(style.Legend.Title.FontSize),
				Height = aes.Name.Height(style.Legend.Title.FontSize)
			};
		}

		Items = new(style);
	}

	public IAestheticMapping Aes { get; }

	public Dimension<string>? Title { get; }

	public Items Items { get; set; }

	public double Width { get; set; }

	public double Height { get; set; }

	public void Add(string label, IElement element)
	{
		var dim = Items.GetOrAdd(label).elements.Add(element);

		Width = Max(Width, dim.Width);
		Height = Max(Height, dim.Height);
	}
}
