using System.Globalization;
using GGNet.Mcp;

namespace GGNet.Evals;

public class ProcessCaptureEvals
{
	[Theory]
	[InlineData(0)]
	[InlineData(7)]
	public async Task DrainsLargeStderrBeforeStdoutAndPreservesExitStatus(int exitCode)
	{
		// Arrange

		var code = exitCode.ToString(CultureInfo.InvariantCulture);
		var line = new string('e', 128);
		var start = Shell(
			$"i=0; while [ $i -lt 4096 ]; do printf '%s\\n' '{line}' >&2; i=$((i+1)); done; printf 'output\\n'; exit {code}",
			$"(for /L %i in (1,1,4096) do @echo {line}) 1>&2 & echo output& exit /b {code}");
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(20));

		// Act

		var result = await ProcessCapture.RunAsync(start, timeout.Token);

		// Assert

		using var _ = new AssertionScope();
		result.ExitStatus.ExitCode.Should().Be(exitCode);
		result.StandardOutput.Trim().Should().Be("output");
		result.StandardError.Split('\n', StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(4096)
			.And.OnlyContain(value => value.TrimEnd('\r') == line);
	}

	[Fact]
	public async Task CancellationBeforeStartDoesNotLaunchTheExecutable()
	{
		// Arrange

		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		var start = new ProcessStartInfo("ggnet-executable-that-does-not-exist");

		// Act

		Func<Task> run = () => ProcessCapture.RunAsync(start, cancellation.Token);

		// Assert

		await run.Should().ThrowAsync<OperationCanceledException>();
	}

	[Theory]
	[InlineData(@"\357\273\277hello")]
	[InlineData(@"\377\376h\000e\000l\000l\000o\000")]
	public async Task PreservesBomAwareTextDecoding(string bytes)
	{
		// Arrange

		Assert.SkipWhen(OperatingSystem.IsWindows(), "This encoding check uses POSIX printf byte escapes.");
		var start = Shell($"printf '{bytes}'; printf '{bytes}' >&2", "");

		// Act

		var result = await ProcessCapture.RunAsync(start, TestContext.Current.CancellationToken);

		// Assert

		using var _ = new AssertionScope();
		result.StandardOutput.Should().Be("hello");
		result.StandardError.Should().Be("hello");
	}

	[Fact]
	public async Task CancellationStopsTheRunningChildBeforeReturning()
	{
		// Arrange

		Assert.SkipWhen(OperatingSystem.IsWindows(), "This process-tree check uses a POSIX shell.");
		var marker = Path.GetTempFileName();
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cancellation.CancelAfter(TimeSpan.FromSeconds(20));
		var start = Shell("sleep 60 & child=$!; printf '%s' \"$child\" > \"$1\"; wait", "");
		start.ArgumentList.Add("ggnet-process-test");
		start.ArgumentList.Add(marker);
		var pending = ProcessCapture.RunAsync(start, cancellation.Token);
		Process? child = null;

		try
		{
			string pid;
			while (string.IsNullOrEmpty(pid = await File.ReadAllTextAsync(marker, cancellation.Token)))
			{
				await Task.Delay(20, cancellation.Token);
			}
			child = Process.GetProcessById(int.Parse(pid, CultureInfo.InvariantCulture));

			// Act

			await cancellation.CancelAsync();
			Func<Task> completion = () => pending;

			// Assert

			await completion.Should().ThrowAsync<OperationCanceledException>();
			await child.WaitForExitAsync(TestContext.Current.CancellationToken)
				.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			child.HasExited.Should().BeTrue();
		}
		finally
		{
			await cancellation.CancelAsync();
			try
			{
				await pending;
			}
			catch (OperationCanceledException)
			{
			}
			finally
			{
				if (child is not null && !child.HasExited)
				{
					child.Kill();
				}
				child?.Dispose();
				File.Delete(marker);
			}
		}
	}

	private static ProcessStartInfo Shell(string unix, string windows) => new(
		OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
		OperatingSystem.IsWindows() ? ["/d", "/c", windows] : ["-c", unix])
	{
		RedirectStandardOutput = true,
		RedirectStandardError = true,
	};
}
