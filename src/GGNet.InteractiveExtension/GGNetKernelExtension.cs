using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

using GGNet.Static;

namespace GGNet.InteractiveExtension;

public class GGNetKernelExtension : IKernelExtension
{
	public Task OnLoadAsync(Kernel kernel)
	{
		Formatter.Register<IData>(async (data, writer) =>
		{
			var theme = data.Theme ?? Theme.Theme.Default();

			theme.FontFamily = "var(--theme-font-family)";
			theme.Plot.Background = theme.Plot.Background with { Fill = "var(--theme-background)" };
			theme.Panel.Background = theme.Panel.Background with { Fill = "var(--theme-background)" };
			theme.Panel.Grid.Major.X.Fill = "var(--theme-scrollbar-background)";
			theme.Panel.Grid.Minor.X.Fill = "var(--theme-scrollbar-background)";
			theme.Panel.Grid.Major.Y.Fill = "var(--theme-scrollbar-background)";
			theme.Panel.Grid.Minor.Y.Fill = "var(--theme-scrollbar-background)";
			theme.Axis.Title.X = theme.Axis.Title.X with { Color = "var(--theme-menu-hover-foreground)" };
			theme.Axis.Text.X = theme.Axis.Text.X with { Color = "var(--theme-foreground)" };
			theme.Axis.Title.Y = theme.Axis.Title.Y with { Color = "var(--theme-menu-hover-foreground)" };
			theme.Axis.Text.Y = theme.Axis.Text.Y with { Color = "var(--theme-foreground)" };
			theme.Legend.Title = theme.Legend.Title with { Color = "var(--theme-menu-hover-foreground)" };
			theme.Legend.Labels = theme.Legend.Labels with { Color = "var(--theme-foreground)" };

			data.Theme = theme;

			var svg = await data.AsStringAsync();

			await writer.WriteAsync(svg);
		}, "text/html");

		return Task.CompletedTask;
	}
}