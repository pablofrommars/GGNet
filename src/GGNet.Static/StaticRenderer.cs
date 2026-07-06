namespace GGNet.Static;

[SuppressMessage("Usage", "BL0006:Do not use RenderTree types", Justification = "GGNet Static")]
internal sealed class StaticRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
	: Renderer(serviceProvider, loggerFactory)
{
	private Exception? unhandledException;

	public new ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId)
		=> base.GetCurrentRenderTreeFrames(componentId);

	public int AttachContainer(ContainerComponent container)
		=> AssignRootComponentId(container);

	public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

	protected override void HandleException(Exception exception)
	{
		unhandledException = exception;
	}

	protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
		=> Task.CompletedTask;

	public async Task Dispatch(Action callback)
	{
		await Dispatcher.InvokeAsync(callback);

		if (unhandledException is null)
		{
			return;
		}

		ExceptionDispatchInfo.Capture(unhandledException).Throw();
	}
}
