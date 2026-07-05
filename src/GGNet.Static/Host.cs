namespace GGNet.Static;

[SuppressMessage("Usage", "BL0006:Do not use RenderTree types", Justification = "<Pending>")]
public sealed class Host
{
	private readonly ServiceCollection _serviceCollection = new();

	private readonly Lazy<IServiceProvider> provider;

	private Host()
	{
		provider = new(() => _serviceCollection.BuildServiceProvider());
	}

	internal async Task RenderAsync(Type type, TextWriter writer, IDictionary<string, object?>? parameters = null)
	{
		// Blazor renderers are single-threaded: attaching root components and reading
		// render trees must not interleave across renders. A renderer per render keeps
		// concurrent renders isolated; the (empty) service provider stays shared.
		var renderer = new StaticRenderer(provider.Value, new NullLoggerFactory());

		try
		{
			var component = new RenderedComponent(renderer);

			await component.RenderAsync(type, parameters is null ? ParameterView.Empty : ParameterView.FromDictionary(parameters));

			component.WriteHTML(writer);
		}
		finally
		{
			await renderer.DisposeAsync();
		}
	}

	private static readonly Lazy<Host> lazy = new(() => new());

	public static Host Instance => lazy.Value;
}
