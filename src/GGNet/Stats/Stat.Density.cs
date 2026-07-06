namespace GGNet;

public static partial class Stat
{
	/// <summary>
	/// Gaussian kernel density estimate over an evenly spaced grid: a source
	/// for <c>Geom_Area</c>/<c>Geom_Line</c> (x: at, y: density).
	/// </summary>
	/// <param name="source">Items to estimate over; recomputed every render pass.</param>
	/// <param name="selector">Value per item, in x-axis data units.</param>
	/// <param name="bandwidth">Kernel bandwidth in data units; null uses Silverman's rule of thumb (<c>Nrd0</c>).</param>
	/// <param name="n">Grid points.</param>
	/// <param name="from">Grid start; null extends three bandwidths below the minimum.</param>
	/// <param name="to">Grid end; null extends three bandwidths above the maximum.</param>
	public static StatSource<DensityPoint> Density<T>(IReadOnlyList<T> source, Func<T, double> selector, double? bandwidth = null, int n = 512, double? from = null, double? to = null)
		=> new(() =>
		{
			var values = Values(source, selector);

			var result = new List<DensityPoint>();

			foreach (var (at, density) in Kde(values, bandwidth, n, from, to))
			{
				result.Add(new(at, density));
			}

			return result;
		});

	/// <summary>
	/// Gaussian kernel density estimate per group, each over its own grid: a
	/// faceted density or violin source (<c>Geom_Violin</c>: x group, y at,
	/// width density). Facet the output on the same key.
	/// </summary>
	/// <param name="source">Items to estimate over; recomputed every render pass.</param>
	/// <param name="selector">Value per item, in the value axis's data units.</param>
	/// <param name="groupBy">Group key per item; estimation runs independently within each group.</param>
	/// <param name="bandwidth">Kernel bandwidth in data units; null uses Silverman's rule per group.</param>
	/// <param name="n">Grid points per group.</param>
	public static StatSource<DensityPoint<TKey>> Density<T, TKey>(IReadOnlyList<T> source, Func<T, double> selector, Func<T, TKey> groupBy, double? bandwidth = null, int n = 512)
		where TKey : notnull
		=> new(() =>
		{
			var groups = Group(source, selector, groupBy);

			var result = new List<DensityPoint<TKey>>();

			foreach (var key in groups.Keys.Order())
			{
				foreach (var (at, density) in Kde(groups[key], bandwidth, n, null, null))
				{
					result.Add(new(key, at, density));
				}
			}

			return result;
		});

	/// <summary>
	/// Silverman's rule-of-thumb bandwidth: 0.9 · min(sd, IQR/1.34) · n^(−1/5).
	/// </summary>
	/// <param name="values">Sample values.</param>
	public static double Nrd0(IReadOnlyList<double> values)
	{
		if (values.Count < 2)
		{
			return 1.0;
		}

		var sd = StandardDeviation(values);

		var sorted = values.Order().ToArray();
		var iqr = Quantile(sorted, 0.75) - Quantile(sorted, 0.25);

		var spread = Math.Min(sd, iqr / 1.34);

		if (spread <= 0.0)
		{
			spread = sd > 0.0 ? sd : 1.0;
		}

		return 0.9 * spread * Math.Pow(values.Count, -0.2);
	}

	private static IEnumerable<(double at, double density)> Kde(List<double> values, double? bandwidth, int n, double? from, double? to)
	{
		if (values.Count == 0 || n < 2)
		{
			yield break;
		}

		var h = bandwidth ?? Nrd0(values);

		var lo = from ?? values.Min() - 3.0 * h;
		var hi = to ?? values.Max() + 3.0 * h;

		var step = (hi - lo) / (n - 1);

		for (var i = 0; i < n; i++)
		{
			var at = lo + i * step;

			var sum = 0.0;

			for (var j = 0; j < values.Count; j++)
			{
				var u = (at - values[j]) / h;

				sum += Math.Exp(-0.5 * u * u);
			}

			yield return (at, sum / (values.Count * h * Math.Sqrt(2.0 * Math.PI)));
		}
	}

	// Type-7 quantile (R default) over a pre-sorted sample.
	private static double Quantile(double[] sorted, double p)
	{
		var h = (sorted.Length - 1) * p;
		var lo = (int)Math.Floor(h);

		if (lo >= sorted.Length - 1)
		{
			return sorted[^1];
		}

		return sorted[lo] + (h - lo) * (sorted[lo + 1] - sorted[lo]);
	}

	private static double StandardDeviation(IReadOnlyList<double> values)
	{
		var mean = 0.0;

		for (var i = 0; i < values.Count; i++)
		{
			mean += values[i];
		}

		mean /= values.Count;

		var ss = 0.0;

		for (var i = 0; i < values.Count; i++)
		{
			var d = values[i] - mean;

			ss += d * d;
		}

		return Math.Sqrt(ss / (values.Count - 1));
	}

	private static List<double> Values<T>(IReadOnlyList<T> source, Func<T, double> selector)
	{
		var values = new List<double>(source.Count);

		for (var i = 0; i < source.Count; i++)
		{
			values.Add(selector(source[i]));
		}

		return values;
	}

	private static Dictionary<TKey, List<double>> Group<T, TKey>(IReadOnlyList<T> source, Func<T, double> selector, Func<T, TKey> groupBy)
		where TKey : notnull
	{
		var groups = new Dictionary<TKey, List<double>>();

		for (var i = 0; i < source.Count; i++)
		{
			var key = groupBy(source[i]);

			if (!groups.TryGetValue(key, out var values))
			{
				values = [];
				groups[key] = values;
			}

			values.Add(selector(source[i]));
		}

		return groups;
	}
}
