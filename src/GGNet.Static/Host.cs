namespace GGNet.Static;

public sealed class Host
{
	private readonly ServiceCollection _serviceCollection = new();

	private readonly Lazy<IServiceProvider> provider;
	private readonly Lazy<StaticRenderer> renderer;

	private Host()
	{
		provider = new(() => _serviceCollection.BuildServiceProvider());

		renderer = new(() => new(provider.Value, new NullLoggerFactory()));
	}

	internal async Task<RenderedComponent> RenderAsync(Type type, IDictionary<string, object?>? parameters = null)
	{
		var component = new RenderedComponent(renderer.Value);

		await component.RenderAsync(type, parameters is null ? ParameterView.Empty : ParameterView.FromDictionary(parameters));

		return component;
	}

	private static readonly Lazy<Host> lazy = new(() => new());

	public static Host Instance => lazy.Value;
}