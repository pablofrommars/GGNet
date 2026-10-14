using Microsoft.JSInterop;

namespace GGNet.Components;

internal sealed record PanelInteropOptions(bool Pan, bool PanX, bool PanY, bool Tooltip);

internal sealed class PanelInterop(IJSRuntime runtime) : IAsyncDisposable
{
	internal const string ModulePath = "./_content/GGNet/Components/Panel.razor.js";
	internal const string InitializeMethod = "initialize";
	internal const string ShowTooltipMethod = "showTooltip";
	internal const string DisposeMethod = "dispose";

	private IJSObjectReference? module;
	private string? id;

	public async ValueTask InitializeAsync<TComponent>(string panelId, ElementReference capture, ElementReference target, DotNetObjectReference<TComponent> dotNetRef, PanelInteropOptions options)
		where TComponent : class
	{
		id = panelId;

		module ??= await runtime.InvokeAsync<IJSObjectReference>("import", ModulePath);

		await module.InvokeVoidAsync(InitializeMethod, panelId, capture, target, dotNetRef, options);
	}

	public async ValueTask ShowTooltipAsync(ElementReference element)
	{
		if (module is null || id is null)
		{
			return;
		}

		try
		{
			await module.InvokeVoidAsync(ShowTooltipMethod, id, element);
		}
		catch (Exception e) when (e is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
		{
		}
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			if (module is not null)
			{
				await module.InvokeVoidAsync(DisposeMethod, id);
				await module.DisposeAsync();
			}
		}
		catch (Exception e) when (e is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
		{
		}
	}
}
