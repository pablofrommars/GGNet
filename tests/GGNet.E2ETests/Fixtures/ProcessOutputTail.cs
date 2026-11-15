using System.Collections.Concurrent;
using System.Diagnostics;

namespace GGNet.E2ETests.Fixtures;

internal sealed class ProcessOutputTail(int capacity = 50)
{
	private readonly ConcurrentQueue<(long index, DateTime at, string text)> lines = new();
	private long written;

	// The index the next line will carry; pass it to Since to read what follows.
	public long Mark => Interlocked.Read(ref written);

	public async Task DrainAsync(Process process, CancellationToken token)
	{
		try
		{
			await foreach (var line in process.ReadAllLinesAsync(token))
			{
				var content = line.Content.Length > 2048 ? line.Content[..2047] + "…" : line.Content;
				lines.Enqueue((Interlocked.Increment(ref written) - 1, DateTime.UtcNow, $"[{(line.StandardError ? "stderr" : "stdout")}] {content}"));
				while (lines.Count > capacity)
				{
					lines.TryDequeue(out _);
				}
			}
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
		}
	}

	// Retained lines from the mark on, each prefixed with its UTC arrival time.
	public string Since(long mark) => string.Join(Environment.NewLine, lines
		.Where(line => line.index >= mark)
		.Select(line => FormattableString.Invariant($"{line.at:HH:mm:ss.fff} {line.text}")));

	public override string ToString() => string.Join(Environment.NewLine, lines.Select(line => line.text));
}
