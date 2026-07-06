namespace GGNet.Headless;

[SuppressMessage("Usage", "BL0006:Do not use RenderTree types", Justification = "GGNet Headless")]
internal sealed class ContainerComponent : IComponent
{
	private readonly HeadlessRenderer renderer;
	private readonly int id;

	public ContainerComponent(HeadlessRenderer renderer)
	{
		this.renderer = renderer;

		id = renderer.AttachContainer(this);
	}

	private RenderHandle handle;

	public void Attach(RenderHandle renderHandle) => handle = renderHandle;

	public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();

	public Task RenderAsync(Type componentType, ParameterView parameters) => renderer.Dispatch(() =>
	{
		handle.Render(builder =>
		{
			builder.OpenComponent(0, componentType);

			foreach (var parameterValue in parameters)
			{
				builder.AddAttribute(1, parameterValue.Name, parameterValue.Value);
			}

			builder.CloseComponent();
		});
	});

	public int Child() => renderer.GetCurrentRenderTreeFrames(id).Array[0].ComponentId;
}
