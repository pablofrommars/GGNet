using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;

namespace GGNet.E2ETests.Fixtures;

// Holds one direction of the Blazor circuit's WebSocket traffic, in order: server frames delay
// DOM delivery, browser messages delay JS-to-.NET invocations. The other direction flows freely.
internal sealed class CircuitGate : IAsyncDisposable
{
	private readonly Channel<(IWebSocketRoute target, IWebSocketFrame frame, bool binary, long arrived)> messages = Channel.CreateUnbounded<(IWebSocketRoute, IWebSocketFrame, bool, long)>();
	private readonly CancellationTokenSource stopped = new();
	private readonly ConcurrentQueue<IWebSocketRoute> routes = new();
	private readonly bool holdInvocations;
	private readonly Task forwarding;
	private TaskCompletionSource received = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private TaskCompletionSource? held;
	private TaskCompletionSource allowanceChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private int allowance;
	private int heldMessages;
	private bool disconnected;
	private int connections;
	private int gatedMessages;
	private int forwardedMessages;
	private long latencyTicks;

	public CircuitGate(bool invocations = false)
	{
		holdInvocations = invocations;
		forwarding = ForwardAsync();
	}

	public async Task InstallAsync(IPage page)
	{
		// After a refused WebSocket, Blazor reconnects through its HTTP fallback transports.
		await page.RouteAsync("**/_blazor**", async route =>
		{
			if (Volatile.Read(ref disconnected))
			{
				await route.AbortAsync();
			}
			else
			{
				await route.FallbackAsync();
			}
		});
		await page.RouteWebSocketAsync("**/_blazor*", RouteCircuit);
	}

	private void RouteCircuit(IWebSocketRoute route)
	{
		Interlocked.Increment(ref connections);
		if (Volatile.Read(ref disconnected))
		{
			_ = route.CloseAsync(new() { Code = 1011, Reason = "Circuit gate disconnected" });
			return;
		}

		var server = route.ConnectToServer();
		routes.Enqueue(route);
		routes.Enqueue(server);

		var source = holdInvocations ? route : server;
		var target = holdInvocations ? server : route;
		var handshakeSent = false;
		source.OnMessage(frame =>
		{
			Interlocked.Increment(ref gatedMessages);
			if (Volatile.Read(ref held) is not null)
			{
				Interlocked.Increment(ref heldMessages);
				Volatile.Read(ref received).TrySetResult();
			}
			// Both sides send a text SignalR handshake, then MessagePack. Playwright's Text
			// getter also decodes binary frames, so it cannot identify the frame type.
			messages.Writer.TryWrite((target, frame, handshakeSent, Stopwatch.GetTimestamp()));
			handshakeSent = true;
		});
	}

	// Forwards every later message a fixed time after it arrived, in order: a constant one-way
	// latency, unlike Hold and Release, which deliver a burst with no delay inside it.
	public void Delay(TimeSpan latency) => Volatile.Write(ref latencyTicks, latency.Ticks);

	public void Hold()
	{
		Volatile.Write(ref received, new(TaskCreationOptions.RunContinuationsAsynchronously));
		Volatile.Write(ref heldMessages, 0);
		Volatile.Write(ref allowance, 0);
		Interlocked.Exchange(ref held, new(TaskCreationOptions.RunContinuationsAsynchronously));
	}

	// Messages that arrived since Hold, released or not.
	public int HeldMessages => Volatile.Read(ref heldMessages);

	public async Task WaitForHeldMessagesAsync(int count)
	{
		var deadline = DateTime.UtcNow.AddSeconds(10);
		while (HeldMessages < count)
		{
			if (DateTime.UtcNow > deadline)
			{
				throw new TimeoutException(FormattableString.Invariant($"Expected {count} held messages, saw {HeldMessages}."));
			}
			await Task.Delay(20, TestContext.Current.CancellationToken);
		}
	}

	// Forwards the next messages in order while the rest stay held.
	public void ReleaseNext(int count)
	{
		Interlocked.Add(ref allowance, count);
		Interlocked.Exchange(ref allowanceChanged, new(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();
	}

	public async Task WaitForHeldMessageAsync()
	{
		try
		{
			var completed = await Task.WhenAny(Volatile.Read(ref received).Task, forwarding).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
			await completed;
		}
		catch (TimeoutException exception)
		{
			throw new TimeoutException(FormattableString.Invariant($"No held {(holdInvocations ? "browser" : "server")} message: connections={Volatile.Read(ref connections)}, received={Volatile.Read(ref gatedMessages)}, forwarded={Volatile.Read(ref forwardedMessages)}."), exception);
		}
	}

	public void Release() => Interlocked.Exchange(ref held, null)?.TrySetResult();

	// Drops held and later messages, closes both ends and refuses reconnection.
	public async Task DisconnectAsync()
	{
		Volatile.Write(ref disconnected, true);
		Release();
		while (routes.TryDequeue(out var route))
		{
			await route.CloseAsync(new() { Code = 1011, Reason = "Circuit gate disconnected" });
		}
	}

	private async Task ForwardAsync()
	{
		try
		{
			await foreach (var (target, frame, binary, arrived) in messages.Reader.ReadAllAsync(stopped.Token))
			{
				var remaining = TimeSpan.FromTicks(Volatile.Read(ref latencyTicks)) - Stopwatch.GetElapsedTime(arrived);
				if (remaining > TimeSpan.Zero)
				{
					await Task.Delay(remaining, stopped.Token);
				}

				while (Volatile.Read(ref held) is { } pause)
				{
					var changed = Volatile.Read(ref allowanceChanged);
					if (TryConsumeAllowance())
					{
						break;
					}
					await Task.WhenAny(pause.Task, changed.Task).WaitAsync(stopped.Token);
				}

				if (Volatile.Read(ref disconnected))
				{
					continue;
				}

				if (binary)
				{
					target.Send(frame.Binary ?? throw new InvalidOperationException("Missing binary Blazor frame payload."));
				}
				else
				{
					target.Send(frame.Text ?? throw new InvalidOperationException("Missing text SignalR handshake."));
				}
				Interlocked.Increment(ref forwardedMessages);
			}
		}
		catch (OperationCanceledException) when (stopped.IsCancellationRequested)
		{
		}
	}

	private bool TryConsumeAllowance()
	{
		var current = Volatile.Read(ref allowance);
		while (current > 0)
		{
			var seen = Interlocked.CompareExchange(ref allowance, current - 1, current);
			if (seen == current)
			{
				return true;
			}
			current = seen;
		}

		return false;
	}

	public async ValueTask DisposeAsync()
	{
		stopped.Cancel();
		messages.Writer.TryComplete();
		await forwarding;
		stopped.Dispose();
	}
}
