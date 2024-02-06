namespace GGNet.Scales.Common;

using static System.Math;

/*
 *  Talbot, Lin, and Hanrahan. An Extension of Wilkinson’s Algorithm for Positioning Tick Labels on Axes, Infovis 2010.
 */

public static class Wilkinson
{
	private static readonly double[] w = [0.25, 0.2, 0.5, 0.05];

	private static double FlooredMod(double a, double n) => a - n * Floor(a / n);

	private static double Pow10(int i)
	{
		var a = 1.0;
		for (int j = 0; j < i; j++)
		{
			a *= 10;
		}

		return a;
	}

	private static readonly double eps = double.Epsilon * 100;

	private static double Simplicity(double[] Q, double q, int j, double lmin, double lmax, double lstep)
	{
		var v = (FlooredMod(lmin, lstep) < eps && lmin <= 0 && lmax >= 0) ? 1 : 0;
		return 1.0 - Array.IndexOf(Q, q) / (Q.Length - 1.0) - j + v;
	}

	private static double MaxSimplicity(double[] Q, double q, int j) =>
		1.0 - Array.IndexOf(Q, q) / (Q.Length - 1.0) - j + 1.0;

	private static double Coverage(double dmin, double dmax, double lmin, double lmax) =>
		1.0 - 0.5 * (((dmax - lmax) * (dmax - lmax) + (dmin - lmin) * (dmin - lmin)) / (0.1 * (dmax - dmin) * (0.1 * (dmax - dmin))));

	private static double MaxCoverage(double dmin, double dmax, double span)
	{
		var range = dmax - dmin;

		if (span > range)
		{
			return 1.0 - 0.25 * (span - range) * (span - range) / (0.01 * range * range);
		}
		else
		{
			return 1.0;
		}
	}

	private static double Density(int k, int m, double dmin, double dmax, double lmin, double lmax)
	{
		var r = -(k - 1.0) / (lmax - lmin);
		var rt = -(m - 1.0) / (Max(lmax, dmax) - Min(dmin, lmin));

		return 2.0 - Max(r / rt, rt / r);
	}

	private static double MaxDensity(int k, int m) => k >= m ? 2.0 - (k - 1.0) / (m - 1.0) : 1.0;

	private static double[]? ExtendedBase(double dmin, double dmax, double[] Q, int m, bool onlyLoose)
	{
		var bscore = -2.0;
		var from = 0.0;
		var to = 0.0;
		var by = 0.0;

		if (dmax - dmin < eps)
		{
			//return(seq(from=dmin, to=dmax, length.out=m))
			return null;
		}

		var j = 1;
		while (j < int.MaxValue)
		{
			foreach (var q in Q)
			{
				var sm = MaxSimplicity(Q, q, j);
				if (w[0] * sm + w[1] + w[2] + w[3] < bscore)
				{
					j = int.MaxValue - 1;
					break;
				}

				var k = 2;
				while (k < int.MaxValue)
				{
					var dm = MaxDensity(k, m);

					if (w[0] * sm + w[1] + w[2] * dm + w[3] < bscore)
						break;

					var delta = (dmax - dmin) / (k + 1.0) / (j * q);
					var z = (int)Ceiling(Log10(delta));

					while (z < int.MaxValue)
					{
						var step = j * q * Pow10(z);
						var cm = MaxCoverage(dmin, dmax, step * (k - 1.0));

						if (w[0] * sm + w[1] * cm + w[2] * dm + w[3] < bscore)
							break;

						for (int start = (int)(Floor(dmax / step - (k - 1)) * j); start <= (int)Ceiling(dmin / step) * j; start++)
						{
							var lmin = start * step / j;
							var lmax = lmin + step * (k - 1);

							var s = Simplicity(Q, q, j, lmin, lmax, step);
							var d = Density(k, m, dmin, dmax, lmin, lmax);
							var c = Coverage(dmin, dmax, lmin, lmax);

							var score = w[0] * s + w[1] * c + w[2] * d + w[3];

							if (score > bscore && (!onlyLoose || (lmin <= dmin && lmax >= dmax)))
							{
								from = lmin;
								to = lmax;
								by = step;
								bscore = score;
							}
						}

						z++;
					}

					k++;
				}
			}

			j++;
		}

		if (by == 0.0)
		{
			return null;
		}

		var n = (int)((to - from) / by) + 1;
		var results = new double[n];

		for (var i = 0; i < n; i++)
		{
			results[i] = from + i * by;
		}

		return results;
	}

	public static double[]? Extended(double dmin, double dmax, int m = 5, bool onlyLoose = false)
		=> ExtendedBase(dmin, dmax, [1.0, 5.0, 2.0, 2.5, 4.0, 3.0], m, onlyLoose);

	public static double[]? Extended(Instant dmin, Instant dmax, int m = 5, bool onlyLoose = false)
	{
		var diff = (dmax - dmin).TotalSeconds;

		var scale = diff switch
		{
			double value when value <= 2 * 60 => 1,
			double value when value <= 2 * 3600 => 60,
			double value when value <= 2 * 86400 => 3600,
			double value when value <= 2 * 604800 => 86400,
			_ => 604800
		};

		var breaks = ExtendedBase(
			dmin: dmin.ToUnixTimeSeconds() / scale,
			dmax: dmax.ToUnixTimeSeconds() / scale,
			Q: [1, 2, 1.5, 4, 3],
			m,
			onlyLoose
		);

		if (breaks is null)
		{
			return null;
		}

    var scaled = new double[breaks.Length];
    for (var i = 0; i < breaks.Length; i++)
    {
      scaled[i] = 1_000.0 * scale * breaks[i];
    }

    return scaled;
	}
}
