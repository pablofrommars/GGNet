using static System.Math;

namespace GGNet;

using Scales;

using static Direction;
using static Guide;

internal sealed class Legends : Buffer<Legend>
{
	private readonly Theme.Theme theme;

	public Legends(Theme.Theme theme) : base(8, 1) => this.theme = theme;

	public Legend GetOrAdd(IAestheticMapping aes)
	{
		for (var i = 0; i < Count; i++)
		{
			var ret = Get(i);
			if (ret.Aes == aes)
			{
				return ret;
			}
		}

		var legend = new Legend(theme, aes);

		Add(legend);

		return legend;
	}

	public (double width, double height) Dimension()
	{
		var width = 0.0;
		var height = 0.0;

		for (var i = 0; i < Count; i++)
		{
			var legend = Get(i);

			if (legend.Title?.Width > 0)
			{
				var w = theme.Legend.Title.Margin.Left + legend.Title.Width + theme.Legend.Title.Margin.Left;
				var h = theme.Legend.Title.Margin.Top + legend.Title.Height + theme.Legend.Title.Margin.Bottom;

				if (theme.Legend.Direction == Vertical)
				{
					width = Max(width, w);
					height += h;
				}
				else
				{
					width += w;
					height = Max(height, h);
				}
			}

			if (legend.Aes.Type == Items)
			{
				for (var j = 0; j < legend.Items.Count; j++)
				{
					var (label, elements) = legend.Items[j];

					var w = legend.Width + theme.Legend.Labels.Margin.Left + label.Width + theme.Legend.Labels.Margin.Right;
					var h = theme.Legend.Labels.Margin.Top + Max(elements.Height, label.Height) + theme.Legend.Labels.Margin.Bottom;

					if (theme.Legend.Direction == Vertical)
					{
						width = Max(width, w);
						height += h;
					}
					else
					{
						width += w;
						height = Max(height, h);
					}
				}
			}
			else if (legend.Aes.Type == ColorBar)
			{
				var n = legend.Items.Count;

				if (theme.Legend.Direction == Vertical)
				{
					height += theme.Legend.Labels.Margin.Top;

					for (var j = 0; j < legend.Items.Count; j++)
					{
						var (label, _) = legend.Items[j];

						var w = legend.Width + theme.Legend.Labels.Margin.Left + label.Width + theme.Legend.Labels.Margin.Right;
						var h = Max(3.0 * legend.Height, label.Height);

						width = Max(width, w);
						height += h;
					}
				}
				else
				{
					width += theme.Legend.Labels.Margin.Left + 3.0 * legend.Width * n;
					height = Max(height, legend.Items[0].label.Height + theme.Legend.Labels.Margin.Bottom + legend.Height);
				}
			}
		}

		return (width, height);
	}
}