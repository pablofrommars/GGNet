using System.Text.Json;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace GGNet.Components.Tests;

public class PanelLifecycleTests : BunitContext
{
	private sealed record P(double X, double Y);

	private static PlotContext<P, double, double> Context(double start = 0)
		=> PlotContext.Build(new[] { new P(start, start), new P(start + 1, start + 1) }, p => p.X, p => p.Y)
			.Geom_Point().Style();

	private IRenderedComponent<Plot<P, double, double>> Mount(ControlledRuntime runtime, InteractivityOptions? options = null)
	{
		Services.AddSingleton<IJSRuntime>(runtime);
		return Render<Plot<P, double, double>>(p => p.Add(x => x.Context, Context())
			.Add(x => x.RenderMode, RenderMode.InteractiveAuto)
			.Add(x => x.Interactivity, options ?? new InteractivityOptions { Pan = true }));
	}

	[Theory]
	[InlineData(false, ZoomAxis.X, false, true, false, null)]
	[InlineData(true, ZoomAxis.X, false, true, false, "pan-y pinch-zoom")]
	[InlineData(true, ZoomAxis.Y, false, false, true, "pan-x pinch-zoom")]
	[InlineData(true, ZoomAxis.Both, false, true, true, "pinch-zoom")]
	[InlineData(true, ZoomAxis.Both, true, true, false, "pan-y pinch-zoom")]
	[InlineData(true, ZoomAxis.Y, true, false, false, null)]
	public void GestureFlagsAndTouchPolicyUseTheSameEffectiveAxes(bool pan, ZoomAxis zoom, bool autoFitY, bool panX, bool panY, string? touchAction)
	{
		// Arrange

		var module = new ControlledModule();
		var options = new InteractivityOptions { Pan = pan, Zoom = zoom, AutoFitY = autoFitY };

		// Act

		Mount(new ControlledRuntime(module), options);

		// Assert

		using var _ = new AssertionScope();
		module.Options.GetProperty("PanX").GetBoolean().Should().Be(panX);
		module.Options.GetProperty("PanY").GetBoolean().Should().Be(panY);
		module.Options.GetProperty("TouchAction").GetString().Should().Be(touchAction);
	}

	[Fact]
	public async Task DisposalDoesNotWaitForImportAndReleasesItsLateResult()
	{
		// Arrange

		var runtime = new ControlledRuntime();
		var module = new ControlledModule();
		var cut = Mount(runtime);
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		await panel.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
		var importPendingAtDisposal = !runtime.Import.Task.IsCompleted;
		runtime.Import.SetResult(module);
		await module.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
		await panel.DisposeAsync();

		// Assert

		using var _ = new AssertionScope();
		importPendingAtDisposal.Should().BeTrue();
		module.Initializations.Should().Be(0);
		module.Disposals.Should().Be(0);
		module.ReferenceDisposals.Should().Be(1);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public async Task InFlightInitializationRetainsCleanupOwnershipAfterDisposal(int failureKind)
	{
		// Arrange

		var module = new ControlledModule(delayInitialization: true);
		var runtime = new ControlledRuntime(module);
		var cut = Mount(runtime);
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;
		await module.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

		// Act

		await panel.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
		var initializationPendingAtDisposal = !module.Initialize.Task.IsCompleted;
		var disposalsBeforeCompletion = module.Disposals;
		var releasesBeforeCompletion = module.ReferenceDisposals;
		if (failureKind < 0)
		{
			module.Initialize.SetResult();
		}
		else
		{
			module.Initialize.SetException(ExpectedFailure(failureKind));
		}
		await module.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
		await panel.DisposeAsync();

		// Assert

		using var _ = new AssertionScope();
		initializationPendingAtDisposal.Should().BeTrue();
		disposalsBeforeCompletion.Should().Be(0);
		releasesBeforeCompletion.Should().Be(0);
		module.Initializations.Should().Be(1);
		module.Disposals.Should().Be(1);
		module.ReferenceDisposals.Should().Be(1);
		Func<Panel<P, double, double>> readReference = () => module.Callback!.Value;
		readReference.Should().Throw<ObjectDisposedException>();
		Renderer.UnhandledException.IsCompleted.Should().BeFalse();
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(2, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	[InlineData(2, true)]
	public void ExpectedInitializationTerminationDoesNotEscape(int failureKind, bool afterImport)
	{
		// Arrange

		var module = new ControlledModule(initializationFailure: ExpectedFailure(failureKind));
		var runtime = afterImport ? new ControlledRuntime(module) : new ControlledRuntime(ExpectedFailure(failureKind));

		// Act

		Action mount = () => Mount(runtime);

		// Assert

		mount.Should().NotThrow();
		using var _ = new AssertionScope();
		module.ReferenceDisposals.Should().Be(afterImport ? 1 : 0);
		module.Disposals.Should().Be(afterImport ? 1 : 0);
		Renderer.UnhandledException.IsCompleted.Should().BeFalse();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void UnexpectedInitializationFailureStillEscapes(bool afterImport)
	{
		// Arrange

		var failure = new InvalidOperationException("Unexpected initialization failure");
		var module = new ControlledModule(initializationFailure: failure);
		var runtime = afterImport ? new ControlledRuntime(module) : new ControlledRuntime(failure);

		// Act

		Action mount = () => Mount(runtime);

		// Assert

		mount.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(failure);
		module.ReferenceDisposals.Should().Be(afterImport ? 1 : 0);
	}

	[Theory]
	[InlineData(0, false)]
	[InlineData(1, false)]
	[InlineData(2, false)]
	[InlineData(0, true)]
	[InlineData(1, true)]
	[InlineData(2, true)]
	public async Task ExpectedCleanupTerminationStillReleasesTheModuleAndCallback(int failureKind, bool referenceFailure)
	{
		// Arrange

		var module = new ControlledModule
		{
			DisposalFailure = referenceFailure ? null : ExpectedFailure(failureKind),
			ReferenceFailure = referenceFailure ? ExpectedFailure(failureKind) : null
		};
		var cut = Mount(new ControlledRuntime(module));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		await panel.DisposeAsync();
		await panel.DisposeAsync();

		// Assert

		using var _ = new AssertionScope();
		module.Disposals.Should().Be(1);
		module.ReferenceDisposals.Should().Be(1);
		Func<Panel<P, double, double>> readReference = () => module.Callback!.Value;
		readReference.Should().Throw<ObjectDisposedException>();
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnexpectedCleanupFailureStillReleasesOwnedResources(bool referenceFailure)
	{
		// Arrange

		var failure = new InvalidOperationException("Unexpected disposal failure");
		var module = new ControlledModule
		{
			DisposalFailure = referenceFailure ? null : failure,
			ReferenceFailure = referenceFailure ? failure : null
		};
		var cut = Mount(new ControlledRuntime(module));
		var panel = cut.FindComponent<Panel<P, double, double>>().Instance;

		// Act

		Func<Task> dispose = () => panel.DisposeAsync().AsTask();

		// Assert

		await dispose.Should().ThrowAsync<InvalidOperationException>().Where(e => ReferenceEquals(e, failure));
		using var _ = new AssertionScope();
		module.Disposals.Should().Be(1);
		module.ReferenceDisposals.Should().Be(1);
		Func<Panel<P, double, double>> readReference = () => module.Callback!.Value;
		readReference.Should().Throw<ObjectDisposedException>();
	}

	[Fact]
	public async Task RetiredPanelCallbacksCannotChangeTheReplacementContext()
	{
		// Arrange

		var module = new ControlledModule();
		var cut = Mount(new ControlledRuntime(module));
		var old = cut.FindComponent<Panel<P, double, double>>().Instance;
		var replacement = Context(100);

		// Act

		await old.DisposeAsync();
		cut.Render(p => p.Add(x => x.Context, replacement));
		var baseline = cut.Markup;
		await cut.InvokeAsync(() => old.OnPanEndAsync(80, 0));
		await cut.InvokeAsync(() => old.OnWheelAsync(350, 200, -1));

		// Assert

		cut.Markup.Should().Be(baseline);
	}

	[Fact]
	public async Task LateCleanupCannotRemoveTheRecreatedPlotsInterop()
	{
		// Arrange

		var owners = new HashSet<string>();
		var oldModule = new ControlledModule(delayInitialization: true, owners: owners);
		var replacement = new ControlledModule(owners: owners);
		var runtime = new ControlledRuntime(oldModule);
		runtime.LaterImports.Enqueue(replacement);
		Services.AddSingleton<IJSRuntime>(runtime);
		var cut = Render<KeyedPlotHost>(p => p.Add(x => x.Context, Context()));
		await oldModule.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

		// Act

		await cut.InvokeAsync(cut.Instance.Replace);
		await replacement.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
		oldModule.Initialize.SetResult();
		await oldModule.Released.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);

		// Assert

		using var _ = new AssertionScope();
		oldModule.InitializationId.Should().NotBe(replacement.InitializationId);
		oldModule.DisposalId.Should().Be(oldModule.InitializationId);
		owners.Should().ContainSingle().Which.Should().Be(replacement.InitializationId);
		replacement.ReferenceDisposals.Should().Be(0);
		oldModule.Options.GetProperty("PanY").GetBoolean().Should().BeFalse();
		replacement.Options.GetProperty("PanY").GetBoolean().Should().BeTrue();
		replacement.Initializations.Should().Be(1);
	}

	private sealed class KeyedPlotHost : ComponentBase
	{
		[Parameter]
		public required PlotContext<P, double, double> Context { get; set; }

		private int generation;

		public void Replace()
		{
			generation++;
			StateHasChanged();
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			builder.OpenComponent<Plot<P, double, double>>(0);
			builder.SetKey(generation);
			builder.AddAttribute(1, nameof(Plot<,,>.Context), Context);
			builder.AddAttribute(2, nameof(Plot<,,>.RenderMode), RenderMode.InteractiveAuto);
			builder.AddAttribute(3, nameof(Plot<,,>.Interactivity), new InteractivityOptions
			{
				Pan = true,
				Zoom = generation == 0 ? ZoomAxis.X : ZoomAxis.Both
			});
			builder.CloseComponent();
		}
	}

	private static Exception ExpectedFailure(int kind) => kind switch
	{
		0 => new JSDisconnectedException("Disconnected"),
		1 => new ObjectDisposedException("Circuit"),
		2 => new TaskCanceledException("Canceled"),
		_ => throw new ArgumentOutOfRangeException(nameof(kind))
	};

	private sealed class ControlledRuntime : IJSRuntime
	{
		public TaskCompletionSource<IJSObjectReference> Import { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Queue<IJSObjectReference> LaterImports { get; } = [];
		private int imports;

		public ControlledRuntime() { }

		public ControlledRuntime(IJSObjectReference module)
		{
			Import.SetResult(module);
		}

		public ControlledRuntime(Exception failure)
		{
			Import.SetException(failure);
		}

		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		{
			if (imports++ > 0 && LaterImports.TryDequeue(out var module))
			{
				return (TValue)(object)module;
			}

			return (TValue)(object)await Import.Task;
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
			=> InvokeAsync<TValue>(identifier, args);
	}

	private sealed class ControlledModule : IJSObjectReference
	{
		public TaskCompletionSource Initialize { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Released { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public DotNetObjectReference<Panel<P, double, double>>? Callback { get; private set; }
		public int Initializations { get; private set; }
		public string? InitializationId { get; private set; }
		public string? DisposalId { get; private set; }
		public JsonElement Options { get; private set; }
		private readonly HashSet<string>? activeOwners;
		public int Disposals { get; private set; }
		public int ReferenceDisposals { get; private set; }
		public Exception? DisposalFailure { get; init; }
		public Exception? ReferenceFailure { get; init; }

		public ControlledModule(bool delayInitialization = false, Exception? initializationFailure = null, HashSet<string>? owners = null)
		{
			activeOwners = owners;

			if (initializationFailure is not null)
			{
				Initialize.SetException(initializationFailure);
			}
			else if (!delayInitialization)
			{
				Initialize.SetResult();
			}
		}

		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
		{
			if (identifier == "initialize")
			{
				Initializations++;
				InitializationId = (string)args![0]!;
				Options = JsonSerializer.SerializeToElement(args[4]);
				activeOwners?.Add(InitializationId);
				Callback = (DotNetObjectReference<Panel<P, double, double>>)args![3]!;
				Started.TrySetResult();
				await Initialize.Task;
			}
			else if (identifier == "dispose")
			{
				Disposals++;
				DisposalId = (string)args![0]!;
				activeOwners?.Remove(DisposalId);
				if (DisposalFailure is not null)
				{
					throw DisposalFailure;
				}
			}

			return default!;
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
			=> InvokeAsync<TValue>(identifier, args);

		public ValueTask DisposeAsync()
		{
			ReferenceDisposals++;
			Released.TrySetResult();
			return ReferenceFailure is not null ? ValueTask.FromException(ReferenceFailure) : ValueTask.CompletedTask;
		}
	}
}
