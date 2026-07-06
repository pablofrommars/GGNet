using GGNet.Components;

using static System.Math;

namespace GGNet.Layout;

// Pure panel geometry: carve strips and axis bands out of the panel's outer
// zone, in exactly the order the markup consumes them. No Blazor, no scales.
internal static class PanelLayout
{
	internal readonly record struct Inputs(
		Zone Outer,
		string? XStripText,
		string? YStripText,
		bool XAxis,
		bool YAxis,
		bool CarveAxes,
		double AxisWidth,
		double AxisHeight,
		double YLabWidth,
		double XLabHeight,
		bool HasXTitles,
		bool HasYTitles,
		Style Style);

	internal readonly record struct Zones(
		Zone Area,
		Zone XStrip,
		Zone YStrip,
		Zone YAxisText,
		Zone YAxisTitle,
		Zone XAxisText,
		Zone XAxisTitle);

	public static Zones Compute(Inputs inputs)
	{
		var style = inputs.Style;

		var area = inputs.Outer;

		Zone xStrip = default;
		Zone yStrip = default;
		Zone yAxisText = default;
		Zone yAxisTitle = default;
		Zone xAxisText = default;
		Zone xAxisTitle = default;

		if (!string.IsNullOrEmpty(inputs.XStripText))
		{
			var width = inputs.XStripText.Width(style.Strip.Text.X.FontSize);
			var height = inputs.XStripText.Height(style.Strip.Text.X.FontSize);

			xStrip.Y = inputs.Outer.Y + style.Strip.Text.X.Margin.Top + height;
			xStrip.Width = style.Strip.Text.X.Margin.Left + width + style.Strip.Text.X.Margin.Right;
			xStrip.Height = style.Strip.Text.X.Margin.Top + height + style.Strip.Text.X.Margin.Bottom;

			area.Y += xStrip.Height;
			area.Height -= xStrip.Height;
		}

		if (!string.IsNullOrEmpty(inputs.YStripText))
		{
			var width = inputs.YStripText.Height(style.Strip.Text.Y.FontSize);
			var height = inputs.YStripText.Width(style.Strip.Text.Y.FontSize);

			yStrip.X = area.X + area.Width - style.Strip.Text.X.Margin.Right - width;
			yStrip.Y = area.Y + style.Strip.Text.Y.Margin.Top;
			yStrip.Width = style.Strip.Text.Y.Margin.Left + width + style.Strip.Text.Y.Margin.Right;
			yStrip.Height = style.Strip.Text.Y.Margin.Top + height + style.Strip.Text.Y.Margin.Bottom;

			area.Width -= yStrip.Width;
		}

		if (inputs.YAxis && inputs.CarveAxes)
		{
			var axisWidth = inputs.AxisWidth;

			if (style.Axis.Y == Position.Left)
			{
				if (inputs.YLabWidth > 0.0 || inputs.HasYTitles)
				{
					yAxisTitle.X = area.X + style.Axis.Title.Y.Margin.Left + inputs.YLabWidth;
					yAxisTitle.Y = area.Y + style.Axis.Title.Y.Margin.Bottom;
					yAxisTitle.Width = style.Axis.Title.Y.Margin.Left + inputs.YLabWidth + style.Axis.Title.Y.Margin.Right;
					yAxisTitle.Height = area.Height;

					area.X += yAxisTitle.Width;
					area.Width -= yAxisTitle.Width;
				}

				yAxisText.X = area.X + style.Axis.Text.Y.Margin.Left + axisWidth;
				yAxisText.Y = area.Y;
				yAxisText.Width = style.Axis.Text.Y.Margin.Left + axisWidth + style.Axis.Text.Y.Margin.Right;

				area.X += yAxisText.Width;
				area.Width -= yAxisText.Width;
			}
			else if (style.Axis.Y == Position.Right)
			{
				if (inputs.YLabWidth > 0.0 || inputs.HasYTitles)
				{
					yAxisTitle.X = area.X + area.Width - inputs.YLabWidth - style.Axis.Title.Y.Margin.Right;
					yAxisTitle.Y = area.Y + style.Axis.Title.Y.Margin.Bottom;
					yAxisTitle.Width = style.Axis.Title.Y.Margin.Left + inputs.YLabWidth + style.Axis.Title.Y.Margin.Right;
					yAxisTitle.Height = area.Height;

					area.Width -= yAxisTitle.Width;
				}

				yAxisText.X = area.X + area.Width - axisWidth;
				yAxisText.Y = area.Y;
				yAxisText.Width = style.Axis.Text.Y.Margin.Left + axisWidth + style.Axis.Text.Y.Margin.Right;

				area.Width -= yAxisText.Width;
			}
		}

		if (inputs.XAxis && inputs.CarveAxes)
		{
			var xTitlesHeight = inputs.XLabHeight;

			if (inputs.HasXTitles)
			{
				xTitlesHeight = Max(xTitlesHeight, style.Axis.Title.X.FontSize.Height());
			}

			if (xTitlesHeight > 0.0)
			{
				xAxisTitle.X = area.X + area.Width - style.Axis.Title.X.Margin.Right;
				xAxisTitle.Y = area.Y + area.Height - style.Axis.Title.X.Margin.Bottom;
				xAxisTitle.Width = style.Axis.Title.X.Margin.Left + area.Width + style.Axis.Title.X.Margin.Right;
				xAxisTitle.Height = style.Axis.Title.X.Margin.Top + xTitlesHeight + style.Axis.Title.X.Margin.Bottom;

				area.Height -= xAxisTitle.Height;
			}

			var axisHeight = inputs.AxisHeight;

			xAxisText.X = area.X;
			xAxisText.Y = area.Y + area.Height - style.Axis.Text.X.Margin.Bottom;
			xAxisText.Width = area.Width;
			xAxisText.Height = style.Axis.Text.X.Margin.Top + axisHeight + style.Axis.Text.X.Margin.Bottom;

			area.Height -= xAxisText.Height;
		}

		if (xStrip.Width > 0)
		{
			xStrip.X = area.X + style.Strip.Text.X.Margin.Left;
		}

		if (yAxisText.Width > 0)
		{
			yAxisText.Height = area.Height;
		}

		return new(area, xStrip, yStrip, yAxisText, yAxisTitle, xAxisText, xAxisTitle);
	}
}
