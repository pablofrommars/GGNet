namespace GGNet.Scales.Common;

using static System.Math;

internal static class Utils
{
	public static double[]? MinorBreaks(double[] b, double min, double max, int n = 2)
	{
		if (b == null || b.Length < 2)
		{
			return null;
		}

		var bd = b[1] - b[0];

		var start = b[0] - (min < b[0] ? bd : 0);
		var end = b[^1] + (max > b[^1] ? bd : 0);

		var by = bd / n;

		var m = (int)Ceiling((end - start) / by);

		// One minor break per step except every n-th, which lands on a major break.
		var minor = new double[m - (m + n - 1) / n];

		var j = 0;
		for (var i = 0; i < m; i++)
		{
			if (i % n == 0)
			{
				continue;
			}

			minor[j++] = start + i * by;
		}

		return minor;
	}
}
