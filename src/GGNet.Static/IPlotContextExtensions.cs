namespace GGNet.Static;

public static class IPlotContextExtensions
{
	private static async Task RenderAsync(IPlotContext context, TextWriter writer, double width = 720, double height = 576)
	{
		var component = await Host.Instance.RenderAsync(context.PlotType, new Dictionary<string, object?>
		{
			["Data"] = context,
			["Width"] = width,
			["Height"] = height
		});

		component.WriteHTML(writer);
	}

	public static Task SaveAsync(this IPlotContext context, string fn, double width = 720, double height = 576)
	{
		using var writer = File.CreateText(fn);

		return RenderAsync(context, writer, width, height);
	}

	public static async Task<string> AsStringAsync(this IPlotContext context, double width = 720, double height = 576)
	{
		using var writer = new StringWriter();

		await RenderAsync(context, writer, width, height);
		
		return writer.ToString();
	}
}