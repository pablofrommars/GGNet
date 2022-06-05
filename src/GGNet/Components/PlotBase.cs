﻿namespace GGNet.Components;

using Theme = Theme.Theme;

public abstract class PlotBase<T, TX, TY> : ComponentBase, IPlot, IPlotRendering, IAsyncDisposable
	where TX : struct
	where TY : struct
{
	[Parameter]
	public Data<T, TX, TY> Data { get; set; } = default!;

	[Parameter]
	public RenderPolicy RenderPolicy { get; set; } = RenderPolicy.Never;

	public string Id => Data.Id;

	public Theme Theme => Data.Theme!;

	protected override void OnInitialized()
	{
		Policy = RenderPolicyBase.Factory(RenderPolicy, this);
	}

	public IRenderPolicy Policy { get; set; } = default!;

	public abstract void Render(RenderTarget target);

	public Task StateHasChangedAsync() => InvokeAsync(StateHasChanged);

	protected override bool ShouldRender() => Policy.ShouldRender();

	protected override void OnAfterRender(bool firstRender) => Policy.OnAfterRender(firstRender);

	public Task RefreshAsync(RenderTarget target) => Policy.RefreshAsync(target);

	public ValueTask DisposeAsync() => Policy.DisposeAsync();
}