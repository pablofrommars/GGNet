using Microsoft.JSInterop;

namespace GGNet.Components;

internal sealed record PanelInteropOptions(bool Pan, bool PanX, bool PanY, bool Tooltip)
{
	public static PanelInteropOptions Create(InteractivityOptions options)
		=> new(options.Pan,
			options.Zoom is ZoomAxis.X or ZoomAxis.Both,
			!options.AutoFitY && options.Zoom is ZoomAxis.Y or ZoomAxis.Both,
			options.CursorTooltip);

	public string? TouchAction => (Pan, PanX, PanY) switch
	{
		(true, true, true) => "pinch-zoom",
		(true, true, false) => "pan-y pinch-zoom",
		(true, false, true) => "pan-x pinch-zoom",
		_ => null
	};
}

internal sealed class PanelInterop(IJSRuntime runtime) : IAsyncDisposable
{
	internal const string ModulePath = "./_content/GGNet/Components/Panel.razor.js";
	internal const string InitializeMethod = "initialize";
	internal const string ShowTooltipMethod = "showTooltip";
	internal const string DisposeMethod = "dispose";

	private IJSObjectReference? module;
	private string? id;
	private readonly string instanceId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
	private readonly Lock gate = new();
	private bool initializing;
	private bool disposed;

	public async ValueTask InitializeAsync<TComponent>(string panelId, ElementReference capture, ElementReference target, DotNetObjectReference<TComponent> dotNetRef, PanelInteropOptions options)
		where TComponent : class
	{
		lock (gate)
		{
			if (disposed || initializing || module is not null)
			{
				return;
			}

			initializing = true;
		}

		IJSObjectReference? acquired = null;
		var started = false;
		var ownerId = panelId + "-" + instanceId;

		try
		{
			acquired = await runtime.InvokeAsync<IJSObjectReference>("import", ModulePath);

			ValueTask initialization;

			lock (gate)
			{
				if (disposed)
				{
					return;
				}

				started = true;
				initialization = acquired.InvokeVoidAsync(InitializeMethod, ownerId, capture, target, dotNetRef, options);
			}

			await initialization;

			lock (gate)
			{
				if (!disposed)
				{
					module = acquired;
					id = ownerId;
					acquired = null;
				}
			}
		}
		catch (Exception e) when (e is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
		{
		}
		finally
		{
			try
			{
				if (acquired is not null)
				{
					await ReleaseAsync(acquired, started ? ownerId : null);
				}
			}
			finally
			{
				lock (gate)
				{
					initializing = false;
				}
			}
		}
	}

	public async ValueTask ShowTooltipAsync(ElementReference element)
	{
		try
		{
			ValueTask showing;

			lock (gate)
			{
				if (disposed || module is null || id is null)
				{
					return;
				}

				showing = module.InvokeVoidAsync(ShowTooltipMethod, id, element);
			}

			await showing;
		}
		catch (Exception e) when (e is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
		{
		}
	}

	public ValueTask DisposeAsync()
	{
		IJSObjectReference? owned;
		string? panelId;

		lock (gate)
		{
			disposed = true;
			owned = module;
			panelId = id;
			module = null;
			id = null;
		}

		return owned is not null ? ReleaseAsync(owned, panelId) : ValueTask.CompletedTask;
	}

	private static async ValueTask ReleaseAsync(IJSObjectReference owned, string? panelId)
	{
		try
		{
			if (panelId is not null)
			{
				await owned.InvokeVoidAsync(DisposeMethod, panelId);
			}
		}
		catch (Exception e) when (e is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
		{
		}
		finally
		{
			try
			{
				await owned.DisposeAsync();
			}
			catch (Exception e) when (e is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
			{
			}
		}
	}
}
