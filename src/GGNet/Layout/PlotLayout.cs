using GGNet.Components;

namespace GGNet.Layout;

using static Position;

// Pure plot geometry: title/subtitle/legend/caption carving of the outer
// canvas, and subdivision of the remaining wrapper into panel zones.
internal static class PlotLayout
{
	internal readonly record struct Inputs(
		double Width,
		double Height,
		string? Title,
		string? SubTitle,
		string? Caption,
		bool HasLegends,
		(double width, double height) LegendsDimension,
		Style Style);

	internal readonly record struct Zones(
		Zone Wrapper,
		Zone Title,
		Zone SubTitle,
		Zone Legend,
		Zone Caption);

	public static Zones Compute(Inputs inputs)
	{
		var style = inputs.Style;

		Zone wrapper = default;
		Zone title = default;
		Zone subTitle = default;
		Zone legend = default;
		Zone caption = default;

		wrapper.X = 0;
		wrapper.Y = 0;
		wrapper.Width = inputs.Width;
		wrapper.Height = inputs.Height;

		if (!string.IsNullOrEmpty(inputs.Title))
		{
			var width = inputs.Title.Height(style.Plot.Title.FontSize);
			var height = inputs.Title.Height(style.Plot.Title.FontSize);

			title.X = style.Plot.Title.Margin.Left;
			title.Y = style.Plot.Title.Margin.Top + height;
			title.Width = style.Plot.Title.Margin.Left + width + style.Plot.Title.Margin.Right;
			title.Height = style.Plot.Title.Margin.Top + height + style.Plot.Title.Margin.Bottom;

			wrapper.Y += title.Height;
			wrapper.Height -= title.Height;
		}

		if (!string.IsNullOrEmpty(inputs.SubTitle))
		{
			var width = inputs.SubTitle.Height(style.Plot.SubTitle.FontSize);
			var height = inputs.SubTitle.Height(style.Plot.SubTitle.FontSize);

			subTitle.X = style.Plot.SubTitle.Margin.Left;
			subTitle.Y = title.Height + style.Plot.SubTitle.Margin.Top + height;
			subTitle.Width = style.Plot.SubTitle.Margin.Left + width + style.Plot.SubTitle.Margin.Right;
			subTitle.Height = style.Plot.SubTitle.Margin.Top + height + style.Plot.SubTitle.Margin.Bottom;

			wrapper.Y += subTitle.Height;
			wrapper.Height -= subTitle.Height;
		}

		if (!string.IsNullOrEmpty(inputs.Caption))
		{
			var width = inputs.Caption.Height(style.Plot.Caption.FontSize);
			var height = inputs.Caption.Height(style.Plot.Caption.FontSize);

			caption.Y = inputs.Height - style.Plot.Caption.Margin.Bottom;
			caption.Width = style.Plot.Caption.Margin.Left + width + style.Plot.Caption.Margin.Right;
			caption.Height = style.Plot.Caption.Margin.Top + height + style.Plot.Caption.Margin.Bottom;

			wrapper.Height -= caption.Height;
		}

		if (inputs.HasLegends)
		{
			var (width, height) = inputs.LegendsDimension;

			if (width > 0 && height > 0)
			{
				if (style.Legend.Position == Right)
				{
					legend.X = inputs.Width - width - style.Legend.Margin.Right;
					legend.Y = wrapper.Y + (wrapper.Height - height) / 2.0;
					legend.Width = style.Legend.Margin.Left + width + style.Legend.Margin.Right;
					legend.Height = wrapper.Height;

					wrapper.Width -= legend.Width;
				}
				else if (style.Legend.Position == Left)
				{
					legend.X = style.Legend.Margin.Left;
					legend.Y = wrapper.Y + (wrapper.Height - height) / 2.0;
					legend.Width = style.Legend.Margin.Left + width + style.Legend.Margin.Right;
					legend.Height = wrapper.Height;

					wrapper.X += legend.Width;
					wrapper.Width -= legend.Width;
				}
				else if (style.Legend.Position == Top)
				{
					legend.X = wrapper.X + (wrapper.Width - width) / 2.0;
					legend.Y = wrapper.Y + style.Legend.Margin.Top;
					legend.Width = wrapper.Width;
					legend.Height = style.Legend.Margin.Top + height + style.Legend.Margin.Bottom;

					wrapper.Y += legend.Height;
					wrapper.Height -= legend.Height;
				}
				else if (style.Legend.Position == Bottom)
				{
					legend.X = wrapper.X + (wrapper.Width - width) / 2.0;
					legend.Y = wrapper.Y + wrapper.Height - height - style.Legend.Margin.Bottom;
					legend.Width = wrapper.Width;
					legend.Height = style.Legend.Margin.Top + height + style.Legend.Margin.Bottom;

					wrapper.Height -= legend.Height;
				}
			}
		}

		if (caption.Width > 0)
		{
			caption.X = wrapper.X + wrapper.Width - style.Plot.Caption.Margin.Right;
		}

		return new(wrapper, title, subTitle, legend, caption);
	}

	internal readonly record struct SubdivisionInputs(
		Zone Wrapper,
		(int rows, int cols) N,
		double Strip,
		(double width, double height) Axis,
		(bool x, bool y) AxisVisibility,
		(double x, double y) AxisTitles,
		(bool x, bool y) AxisTitlesVisibility,
		bool HasXLab,
		Style Style);

	public static List<Zone> Subdivide(SubdivisionInputs inputs, IReadOnlyList<(int row, int col, double width, double height)> panels)
	{
		var style = inputs.Style;
		var wrapper = inputs.Wrapper;

		var zones = new List<Zone>(panels.Count);

		var width = wrapper.Width - inputs.Strip - (inputs.N.cols - 1.0) * style.Panel.Spacing.X;

		if (!inputs.AxisVisibility.y)
		{
			width -= inputs.Axis.width;
		}

		var height = wrapper.Height - inputs.Strip - (inputs.N.rows - 1.0) * style.Panel.Spacing.Y;

		if (!inputs.AxisVisibility.x)
		{
			height -= inputs.Axis.height;
		}

		if (!inputs.AxisTitlesVisibility.x && inputs.HasXLab)
		{
			height -= inputs.AxisTitles.x;
		}

		var xOffsetReset = wrapper.X;

		var xOffset = xOffsetReset;
		var yOffset = wrapper.Y;

		for (var i = 0; i < panels.Count; i++)
		{
			var panel = panels[i];

			var x = xOffset;
			var y = yOffset;

			var w = panel.width * width;
			var h = panel.height * height;

			if (panel.row == 0)
			{
				h += inputs.Strip;
			}

			if (panel.col == 0 && !inputs.AxisVisibility.y && style.Axis.Y == Left)
			{
				w += inputs.Axis.width;
			}

			if (panel.row == (inputs.N.rows - 1))
			{
				if (!inputs.AxisVisibility.x)
				{
					h += inputs.Axis.height;
				}

				if (!inputs.AxisTitlesVisibility.x && inputs.HasXLab)
				{
					h += inputs.AxisTitles.x;
				}
			}

			if (panel.col == (inputs.N.cols - 1))
			{
				w += inputs.Strip;

				if (!inputs.AxisVisibility.y && style.Axis.Y == Right)
				{
					w += inputs.Axis.width;
				}

				xOffset = xOffsetReset;
				yOffset += h + style.Panel.Spacing.Y;
			}
			else
			{
				xOffset += w + style.Panel.Spacing.X;
			}

			zones.Add(new Zone { X = x, Y = y, Width = w, Height = h });
		}

		return zones;
	}
}
