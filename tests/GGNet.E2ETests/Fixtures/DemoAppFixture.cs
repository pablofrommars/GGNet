using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace GGNet.E2ETests.Fixtures;

// Hosts the demo app (real Kestrel, real circuit) and a Playwright Chromium
// for the JS smoke layer. Deliberately opt-in: without GGNET_E2E=1 (or with
// no browser installed) every test skips, so the default `dotnet test` gate
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

	public async Task InitializeAsync()
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

		app = Process.Start(info);

		using var client = new HttpClient();

		for (var i = 0; i < 60; i++)
		{
			try
			{
				var response = await client.GetAsync(BaseUrl);

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

		Reason = "The demo app did not start within 30s (is the solution built?)";
	}

	public async Task DisposeAsync()
	{
		if (browser is not null)
		{
			await browser.DisposeAsync();
		}

		playwright?.Dispose();

		if (app is not null && !app.HasExited)
		{
			app.Kill(entireProcessTree: true);
		}

		app?.Dispose();
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
