namespace GGNet.Headless;

public sealed class RenderedComponent
{
	private readonly HeadlessRenderer renderer;
	private readonly ContainerComponent container;
	private int id;

	internal RenderedComponent(HeadlessRenderer renderer)
	{
		this.renderer = renderer;
		container = new(this.renderer);
	}

	internal async Task RenderAsync(Type type, ParameterView parameters)
	{
		await container.RenderAsync(type, parameters);

		id = container.Child();
	}

	public void WriteHTML(TextWriter writer) => SVGRenderer.Render(renderer, id, writer);
}
