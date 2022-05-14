using static System.Math;

namespace GGNet;

using Elements;
using Scales;

internal sealed class Legend
{
	public Legend(Theme.Theme theme, IAestheticMapping aes)
	{
		Aes = aes;

		if (!string.IsNullOrEmpty(aes.Name))
		{
			Title = new Dimension<string>
			{
				Value = aes.Name,
				Width = aes.Name.Width(theme.Legend.Title.Size),
				Height = aes.Name.Height(theme.Legend.Title.Size)
			};
		}

		Items = new _Items(theme);
	}

	public IAestheticMapping Aes { get; }

	public sealed class Dimension<TValue>
	{
		public TValue Value { get; set; } = default!;

		public double Width { get; set; }

		public double Height { get; set; }
	}

	public Dimension<string>? Title { get; }

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

	internal sealed class _Items : Buffer<(Dimension<string> label, Elements elements)>
	{
		private readonly Theme.Theme theme;

		public _Items(Theme.Theme theme) : base(8, 1) => this.theme = theme;

		public (Dimension<string> label, Elements elements) GetOrAdd(string label)
		{
			for (var i = 0; i < Count; i++)
			{
				var ret = Get(i);
				if (ret.label.Value == label)
				{
					return ret;
				}
			}

			var height = label.Height(theme.Legend.Labels.Size);

			var item = (
				label: new Dimension<string>
				{
					Value = label,
					Width = label.Width(theme.Legend.Labels.Size),
					Height = height
				},
				elements: new Elements(height)
			);

			Add(item);

			return item;
		}
	}

	public _Items Items { get; set; }

	public double Width { get; set; }

	public double Height { get; set; }

	public void Add(string label, IElement element)
	{
		var dim = Items.GetOrAdd(label).elements.Add(element);

		Width = Max(Width, dim.Width);
		Height = Max(Height, dim.Height);
	}
}