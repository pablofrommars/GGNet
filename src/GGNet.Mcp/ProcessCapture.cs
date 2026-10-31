using System.Diagnostics;

namespace GGNet.Mcp;

internal static class ProcessCapture
{
	public static async Task<ProcessTextOutput> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		using var process = Process.Start(startInfo)
			?? throw new InvalidOperationException("The validation process did not start.");

		try
		{
			var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
			var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
			await Task.WhenAll(stdout, stderr);
			var status = await process.WaitForExitStatusAsync(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();

			return new ProcessTextOutput(status, await stdout, await stderr, process.Id);
		}
		finally
		{
			if (!process.HasExited)
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch (InvalidOperationException) when (process.HasExited)
				{
				}

				await process.WaitForExitAsync(CancellationToken.None);
			}
		}
	}
}
