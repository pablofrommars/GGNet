namespace GGNet.Static;

public static class IDataExtensions
{
	private static async Task RenderAsync(IData data, TextWriter writer, double width = 720, double height = 576)
	{
		var component = await Host.Instance.RenderAsync(data.PlotType, new Dictionary<string, object?>
		{
			["Data"] = data,
			["Width"] = width,
			["Height"] = height
		});

		component.WriteHTML(writer);
	}

	public static Task SaveAsync(this IData data, string fn, double width = 720, double height = 576)
	{
		using var writer = File.CreateText(fn);
		return RenderAsync(data, writer, width, height);
	}

	public static async Task<string> AsStringAsync(this IData data, double width = 720, double height = 576)
	{
		using var writer = new StringWriter();
		await RenderAsync(data, writer, width, height);
		return writer.ToString();
	}
}