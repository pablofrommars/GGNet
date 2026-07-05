namespace GGNet.Static;

public static class IPlotContextExtensions
{
	private static Task RenderAsync(IPlotContext context, TextWriter writer, double width = 720, double height = 576, string theme = "default")
	{
		return Host.Instance.RenderAsync(context.PlotType, writer, new Dictionary<string, object?>
		{
			["Context"] = context,
			["Width"] = width,
			["Height"] = height,
			["Theme"] = theme
		});
	}

	public static async Task SaveAsync(this IPlotContext context, string fn, double width = 720, double height = 576, string theme = "default")
	{
		using var writer = File.CreateText(fn);

		await RenderAsync(context, writer, width, height, theme);
	}

	public static async Task<string> AsStringAsync(this IPlotContext context, double width = 720, double height = 576, string theme = "default")
	{
		using var writer = new StringWriter();

		await RenderAsync(context, writer, width, height, theme);

		return writer.ToString();
	}
}
