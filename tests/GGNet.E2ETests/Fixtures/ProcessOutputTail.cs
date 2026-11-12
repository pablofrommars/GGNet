using System.Collections.Concurrent;
using System.Diagnostics;

namespace GGNet.E2ETests.Fixtures;

internal sealed class ProcessOutputTail
{
	private readonly ConcurrentQueue<string> lines = new();

	public async Task DrainAsync(Process process, CancellationToken token)
	{
		try
		{
			await foreach (var line in process.ReadAllLinesAsync(token))
			{
				var content = line.Content.Length > 2048 ? line.Content[..2047] + "…" : line.Content;
				lines.Enqueue($"[{(line.StandardError ? "stderr" : "stdout")}] {content}");
				if (lines.Count > 50)
				{
					lines.TryDequeue(out _);
				}
			}
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
		}
	}

	public override string ToString() => string.Join(Environment.NewLine, lines);
}
