using GGNet.Common;
using GGNet.Elements;
using GGNet.Scales;

using static System.Math;

namespace GGNet.Data;

internal sealed class Legend
{
	public Legend(Theme.Theme theme, IAestheticMapping aes)
	{
		Aes = aes;

		if (!string.IsNullOrEmpty(aes.Name))
		{
			Title = new()
			{
				Value = aes.Name,
				Width = aes.Name.Width(theme.Legend.Title.Size),
				Height = aes.Name.Height(theme.Legend.Title.Size)
			};
		}

		Items = new(theme);
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