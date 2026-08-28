namespace GGNet.Headless;

public static class IPlotContextExtensions
{
	// The export is a one-shot render with no interactivity to schedule, so it
	// names its render mode rather than inheriting the parameter's default.
	internal static Dictionary<string, object?> Parameters(IPlotContext context, double width, double height, string theme) => new()
	{
		["Context"] = context,
		["Width"] = width,
		["Height"] = height,
		["Theme"] = theme,
		["RenderMode"] = RenderMode.Static
	};

	private static Task RenderAsync(IPlotContext context, TextWriter writer, double width = 720, double height = 576, string theme = "default")
	{
		return Host.Instance.RenderAsync(context.PlotType, writer, Parameters(context, width, height, theme));
	}

	/// <summary>Renders the plot to a file as pure SVG.</summary>
	/// <param name="fn">Destination path.</param>
	/// <param name="width">Canvas width in pixels.</param>
	/// <param name="height">Canvas height in pixels.</param>
	/// <param name="theme">Theme name; styles the output via css classes.</param>
	/// <param name="selfContained">Embed the bundled theme's css as a &lt;style&gt; element so the file renders standalone; off by default — app-hosted output is styled by the app's stylesheet.</param>
	public static async Task SaveAsync(this IPlotContext context, string fn, double width = 720, double height = 576, string theme = "default", bool selfContained = false)
	{
		using var writer = File.CreateText(fn);

		await writer.WriteAsync(await AsStringAsync(context, width, height, theme, selfContained));
	}

	/// <summary>Renders the plot to a pure-SVG string.</summary>
	/// <param name="width">Canvas width in pixels.</param>
	/// <param name="height">Canvas height in pixels.</param>
	/// <param name="theme">Theme name; styles the output via css classes.</param>
	/// <param name="selfContained">Embed the bundled theme's css as a &lt;style&gt; element so the string renders standalone; off by default — app-hosted output is styled by the app's stylesheet.</param>
	public static async Task<string> AsStringAsync(this IPlotContext context, double width = 720, double height = 576, string theme = "default", bool selfContained = false)
	{
		using var writer = new StringWriter();

		await RenderAsync(context, writer, width, height, theme);

		var svg = writer.ToString();

		if (!selfContained)
		{
			return svg;
		}

		var insertAt = svg.IndexOf('>', svg.IndexOf("<svg", StringComparison.Ordinal)) + 1;

		return svg[..insertAt] + "\n<style>\n" + ThemeCss.SelfContained(theme) + "</style>" + svg[insertAt..];
	}
}
