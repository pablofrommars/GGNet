using static System.Math;

namespace GGNet.Scales;

public static class Utils
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

		var minor = new double[m - b.Length + 1];

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