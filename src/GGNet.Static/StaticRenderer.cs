namespace GGNet.Static;

[SuppressMessage("Usage", "BL0006:Do not use RenderTree types", Justification = "<Pending>")]
internal sealed class StaticRenderer : Renderer
{
	private Exception? unhandledException;

	private TaskCompletionSource<object> nextRenderTcs = new();

	public StaticRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
		: base(serviceProvider, loggerFactory)
	{
	}

	public new ArrayRange<RenderTreeFrame> GetCurrentRenderTreeFrames(int componentId)
		=> base.GetCurrentRenderTreeFrames(componentId);

	public int AttachContainer(ContainerComponent container)
		=> AssignRootComponentId(container);

	public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

	public Task NextRender => nextRenderTcs.Task;

	protected override void HandleException(Exception exception)
	{
		unhandledException = exception;
	}

	protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
	{
		//var prevTcs = nextRenderTcs;
		nextRenderTcs = new TaskCompletionSource<object>();
		//prevTcs.SetResult(default);
		return Task.CompletedTask;
	}

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