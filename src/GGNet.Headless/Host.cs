namespace GGNet.Headless;

public sealed class Host
{
	private readonly ServiceCollection serviceCollection = new();

	private readonly Lazy<IServiceProvider> provider;

	private Host()
	{
		provider = new(() => serviceCollection.BuildServiceProvider());
	}

	internal async Task RenderAsync(Type type, TextWriter writer, IDictionary<string, object?>? parameters = null)
	{
		// Blazor renderers are single-threaded: rendering and reading the result
		// must not interleave across exports. A renderer per export keeps
		// concurrent exports isolated; the (empty) service provider stays shared.
		await using var renderer = new HtmlRenderer(provider.Value, NullLoggerFactory.Instance);

		var html = await renderer.Dispatcher.InvokeAsync(async () =>
		{
			var component = await renderer.RenderComponentAsync(type, parameters is null ? ParameterView.Empty : ParameterView.FromDictionary(parameters));

			return component.ToHtmlString();
		});

		SvgExtractor.Write(html, writer);
	}

	private static readonly Lazy<Host> lazy = new(() => new());

	public static Host Instance => lazy.Value;
}
