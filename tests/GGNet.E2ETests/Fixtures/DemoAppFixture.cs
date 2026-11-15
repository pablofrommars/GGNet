using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace GGNet.E2ETests.Fixtures;

// Hosts the demo app (real Kestrel, real circuit) and a Playwright Chromium
// for the JS smoke layer. Deliberately opt-in: without GGNET_E2E=1 (or with
// no browser installed) its browser tests skip, so the default `dotnet test` gate
// needs neither a browser nor a port.
public sealed class DemoAppFixture : IAsyncLifetime
{
#if DEBUG
	private const string Configuration = "Debug";
#else
	private const string Configuration = "Release";
#endif

	public bool Available { get; private set; }

	public string Reason { get; private set; } = "GGNET_E2E is not set to 1";

	public string BaseUrl { get; private set; } = default!;

	public IBrowser Browser => browser ?? throw new InvalidOperationException("Fixture not initialized");

	private IPlaywright? playwright;
	private IBrowser? browser;
	private Process? app;
	// Large enough to hold the demo's circuit and connection logging for one test page.
	private readonly ProcessOutputTail output = new(2000);
	private readonly CancellationTokenSource outputCancellation = new();
	private Task outputDrain = Task.CompletedTask;

	public async ValueTask InitializeAsync()
	{
		if (Environment.GetEnvironmentVariable("GGNET_E2E") != "1")
		{
			return;
		}

		try
		{
			playwright = await Playwright.CreateAsync();
			browser = await playwright.Chromium.LaunchAsync();
		}
		catch (Exception e)
		{
			Reason = $"Chromium unavailable: {e.Message}";
			return;
		}

		BaseUrl = $"http://127.0.0.1:{FreePort()}";

		var info = new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"run --project src/GGNet.Demo --no-build --no-launch-profile --configuration {Configuration}",
			WorkingDirectory = RepoRoot(),
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		info.Environment["ASPNETCORE_URLS"] = BaseUrl;
		info.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
		info.Environment["DetailedErrors"] = "true";
		info.Environment["Logging__LogLevel__Microsoft.AspNetCore.Components.Server.Circuits"] = "Debug";
		info.Environment["Logging__LogLevel__Microsoft.AspNetCore.SignalR"] = "Debug";
		info.Environment["Logging__LogLevel__Microsoft.AspNetCore.Http.Connections"] = "Debug";

		app = Process.Start(info) ?? throw new InvalidOperationException("The demo process did not start.");
		outputDrain = output.DrainAsync(app, outputCancellation.Token);

		using var client = new HttpClient();

		for (var i = 0; i < 60; i++)
		{
			if (app.HasExited)
			{
				outputCancellation.CancelAfter(TimeSpan.FromSeconds(1));
				await outputDrain;
				Reason = $"The demo process exited before becoming ready.{Environment.NewLine}{output}";
				return;
			}

			if (outputDrain.IsFaulted)
			{
				await outputDrain;
			}

			try
			{
				using var response = await client.GetAsync(BaseUrl);

				if (response.IsSuccessStatusCode)
				{
					Available = true;
					return;
				}
			}
			catch (HttpRequestException)
			{
			}

			await Task.Delay(500);
		}

		Reason = $"The demo app did not start within 30s (is the solution built?){Environment.NewLine}{output}";
	}

	// Writes the demo's server output since this call to the current test's output when the page
	// reports a Blazor circuit error, so a failing case shows what the server saw. Dispose the
	// returned attachment inside the test: the output helper rejects writes once the test has ended.
	public IAsyncDisposable AttachServerOutputOnCircuitError(IPage page)
	{
		var attachment = new ServerOutputAttachment(output, TestContext.Current.TestOutputHelper);
		page.Console += (_, message) =>
		{
			if (message.Type == "error" && IsCircuitError(message.Text))
			{
				attachment.Report(message.Text);
			}
		};

		return attachment;
	}

	private sealed class ServerOutputAttachment : IAsyncDisposable
	{
		private readonly TaskCompletionSource<string> error = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly Task written;

		public ServerOutputAttachment(ProcessOutputTail output, ITestOutputHelper? test)
		{
			written = WriteAsync(output, output.Mark, test);
		}

		public void Report(string text) => error.TrySetResult(text);

		private async Task WriteAsync(ProcessOutputTail output, long mark, ITestOutputHelper? test)
		{
			var text = await error.Task;
			if (test is null)
			{
				return;
			}

			// The server logs its side of a drop shortly after the browser reports it.
			await Task.Delay(TimeSpan.FromSeconds(1));
			test.WriteLine($"Circuit error in the browser: {text}{Environment.NewLine}Demo server output since the page opened:{Environment.NewLine}{output.Since(mark)}");
		}

		// Waits for a reported error's write; without one, nothing is pending and a later report is ignored.
		public async ValueTask DisposeAsync()
		{
			error.TrySetCanceled();
			try
			{
				await written;
			}
			catch (OperationCanceledException)
			{
			}
		}
	}

	internal static bool IsCircuitError(string text)
		=> text.Contains("Connection disconnected", StringComparison.OrdinalIgnoreCase)
			|| text.Contains("WebSocket closed", StringComparison.OrdinalIgnoreCase)
			|| text.Contains("circuit", StringComparison.OrdinalIgnoreCase);

	public async ValueTask DisposeAsync()
	{
		try
		{
			if (browser is not null)
			{
				await browser.DisposeAsync();
			}
		}
		finally
		{
			try
			{
				playwright?.Dispose();
			}
			finally
			{
				await StopAppAsync();
			}
		}
	}

	private async Task StopAppAsync()
	{
		try
		{
			if (app is not null && !app.HasExited)
			{
				try
				{
					app.Kill(entireProcessTree: true);
				}
				catch (InvalidOperationException) when (app.HasExited)
				{
				}

				await app.WaitForExitAsync();
			}
		}
		finally
		{
			await outputCancellation.CancelAsync();
			try
			{
				await outputDrain;
			}
			finally
			{
				app?.Dispose();
				outputCancellation.Dispose();
			}
		}
	}

	private static int FreePort()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);

		listener.Start();

		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}

	private static string RepoRoot([CallerFilePath] string path = "")
	{
		var directory = new DirectoryInfo(Path.GetDirectoryName(path)!);

		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "GGNet.slnx")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName ?? throw new InvalidOperationException("GGNet.slnx not found above the test source");
	}
}
