using System.Diagnostics;

namespace GGNet.E2ETests;

public class ProcessOutputTailTests
{
	[Fact]
	public async Task RetainsBothOutputStreamsWithTheirLabels()
	{
		// Arrange

		using var process = Process.Start(Shell("printf 'hello\\n'; printf 'problem\\n' >&2", "echo hello& echo problem 1>&2"))!;
		var output = new ProcessOutputTail();
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(20));

		// Act

		await output.DrainAsync(process, timeout.Token);
		await process.WaitForExitAsync(timeout.Token);

		// Assert

		using var _ = new AssertionScope();
		output.ToString().Should().Contain("[stdout] hello").And.Contain("[stderr] problem");
		process.ExitCode.Should().Be(0);
	}

	[Fact]
	public async Task DrainsLargeOutputWhileBoundingRetainedLinesAndLineLength()
	{
		// Arrange

		var line = new string('x', 3000);
		using var process = Process.Start(Shell(
			$"i=0; while [ $i -lt 100 ]; do printf '%s\\n' '{line}' >&2; i=$((i+1)); done",
			$"(for /L %i in (1,1,100) do @echo {line}) 1>&2"))!;
		var output = new ProcessOutputTail();
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(20));

		// Act

		try
		{
			await output.DrainAsync(process, timeout.Token);
			await process.WaitForExitAsync(timeout.Token);
		}
		finally
		{
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
			}
		}

		// Assert

		using var _ = new AssertionScope();
		output.ToString().Split(Environment.NewLine).Should().HaveCount(50)
			.And.OnlyContain(value => value == "[stderr] " + new string('x', 2047) + "…");
		process.ExitCode.Should().Be(0);
	}

	[Fact]
	public async Task SinceReturnsOnlyTheLinesAfterTheMarkWithTheirArrivalTime()
	{
		// Arrange

		var output = new ProcessOutputTail(10);
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		timeout.CancelAfter(TimeSpan.FromSeconds(20));
		using (var first = Process.Start(Shell("printf 'before\\n'", "echo before"))!)
		{
			await output.DrainAsync(first, timeout.Token);
			await first.WaitForExitAsync(timeout.Token);
		}
		var mark = output.Mark;

		// Act

		using (var second = Process.Start(Shell("printf 'after\\n'", "echo after"))!)
		{
			await output.DrainAsync(second, timeout.Token);
			await second.WaitForExitAsync(timeout.Token);
		}
		var since = output.Since(mark);

		// Assert

		using var _ = new AssertionScope();
		since.Should().MatchRegex(@"^\d{2}:\d{2}:\d{2}\.\d{3} \[stdout\] after$");
		output.ToString().Should().Be("[stdout] before" + Environment.NewLine + "[stdout] after");
	}

	private static ProcessStartInfo Shell(string unix, string windows) => new(
		OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
		OperatingSystem.IsWindows() ? ["/d", "/c", windows] : ["-c", unix])
	{
		RedirectStandardOutput = true,
		RedirectStandardError = true,
	};
}
