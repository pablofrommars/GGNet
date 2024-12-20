namespace GGNet.Static;

public static class IPlotContextExtensions
{
	private static async Task RenderAsync(IPlotContext context, TextWriter writer, double width = 720, double height = 576, string theme = "default")
	{
		var component = await Host.Instance.RenderAsync(context.PlotType, new Dictionary<string, object?>
		{
			["Context"] = context,
			["Width"] = width,
			["Height"] = height,
      ["Theme"] = theme
		});

		component.WriteHTML(writer);
	}

	public static Task SaveAsync(this IPlotContext context, string fn, double width = 720, double height = 576, string theme = "default")
	{
		using var writer = File.CreateText(fn);

		return RenderAsync(context, writer, width, height, theme);
	}

	public static async Task<string> AsStringAsync(this IPlotContext context, double width = 720, double height = 576, string theme = "default")
	{
		using var writer = new StringWriter();

		await RenderAsync(context, writer, width, height, theme);

		return writer.ToString();
	}
}
