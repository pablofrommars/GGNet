using GGNet.Exceptions;

namespace GGNet;

public static partial class Stat
{
	/// <summary>
	/// Gaussian kernel density estimate over an evenly spaced grid: a source
	/// for <c>Geom_Area</c>/<c>Geom_Line</c> (x: at, y: density).
	/// </summary>
	/// <param name="source">Items to estimate over; recomputed every render pass.</param>
	/// <param name="selector">Finite value per item, in the value axis's data units; nonfinite values are rejected at calculation or recomputation.</param>
	/// <param name="bandwidth">Kernel bandwidth in data units; an explicit value must be finite and strictly positive, validated at call time even for empty input or fewer than two grid points. Null uses Silverman's rule of thumb (<c>Nrd0</c>, per group when grouped).</param>
	/// <param name="n">Grid points (per group when grouped).</param>
	/// <param name="from">Finite grid start, pinned exactly; null extends three bandwidths below the minimum. Nonfinite explicit bounds are rejected at call time even for empty input or fewer than two grid points.</param>
	/// <param name="to">Finite grid end, pinned exactly; null extends three bandwidths above the maximum. Descending grids and repeated rounded coordinates are allowed.</param>
	public static StatSource<DensityPoint> Density<T>(IReadOnlyList<T> source, Func<T, double> selector, double? bandwidth = null, int n = 512, double? from = null, double? to = null)
	{
		ValidateDensityBandwidth(bandwidth);
		ValidateDensityBound(from, nameof(from));
		ValidateDensityBound(to, nameof(to));

		return new(source as IStatSource, () =>
		{
			var values = Values(source, selector);

			var result = new List<DensityPoint>();

			if (values.Count == 0 || n < 2)
			{
				return result;
			}

			var grid = PrepareDensityGrid(values, bandwidth, n, from, to);

			foreach (var (at, density) in Kde(grid))
			{
				result.Add(new(at, density));
			}

			return result;
		});
	}

	/// <summary>
	/// Gaussian kernel density estimate per group, each over its own grid: a
	/// faceted density or violin source (<c>Geom_Violin</c>: x group, y at,
	/// width density). Facet the output on the same key.
	/// </summary>
	/// <param name="source">Items to estimate over; recomputed every render pass.</param>
	/// <param name="selector">Finite value per item, in the value axis's data units; nonfinite values are rejected at calculation or recomputation.</param>
	/// <param name="groupBy">Group key per item; estimation runs independently within each group.</param>
	/// <param name="bandwidth">Kernel bandwidth in data units; an explicit value must be finite and strictly positive, validated at call time even for empty input or fewer than two grid points. Null uses Silverman's rule of thumb (<c>Nrd0</c>, per group when grouped).</param>
	/// <param name="n">Grid points (per group when grouped).</param>
	public static StatSource<DensityPoint<TKey>> Density<T, TKey>(IReadOnlyList<T> source, Func<T, double> selector, Func<T, TKey> groupBy, double? bandwidth = null, int n = 512)
		where TKey : notnull
	{
		ValidateDensityBandwidth(bandwidth);

		return new(source as IStatSource, () =>
		{
			var groups = Group(source, selector, groupBy);

			var result = new List<DensityPoint<TKey>>();
			var grids = new List<(TKey key, DensityGrid grid)>();

			if (n < 2)
			{
				return result;
			}

			foreach (var key in groups.Keys.Order())
			{
				grids.Add((key, PrepareDensityGrid(groups[key], bandwidth, n, null, null)));
			}

			foreach (var (key, grid) in grids)
			{
				foreach (var (at, density) in Kde(grid))
				{
					result.Add(new(key, at, density));
				}
			}

			return result;
		});
	}

	/// <summary>
	/// Silverman's rule-of-thumb bandwidth: 0.9 · min(sd, IQR/1.34) · n^(−1/5).
	/// Fewer than two finite samples return 1; zero IQR uses sd, or 1 for a
	/// constant sample. Nonfinite samples and bandwidths that cannot be represented
	/// as positive finite doubles throw <see cref="GGNetUserException"/>.
	/// </summary>
	/// <param name="values">Finite sample values.</param>
	public static double Nrd0(IReadOnlyList<double> values)
	{
		for (var i = 0; i < values.Count; i++)
		{
			ValidateStatNumber(values[i], "Sample value");
		}

		if (values.Count < 2)
		{
			return 1.0;
		}

		var (_, sd, sdExponent) = MeanSd(values);
		var sorted = values.Order().ToArray();
		var (iqr, exponent) = InterquartileRange(sorted, sdExponent != 0);
		var spread = iqr / 1.34;

		if (spread <= 0.0 || ScaledLessThan(sd, sdExponent, spread, exponent))
		{
			spread = sd;
			exponent = sdExponent;
		}

		if (spread <= 0.0)
		{
			spread = 1.0;
			exponent = 0;
		}

		var spreadPower = Math.ILogB(spread);
		spread = Math.ScaleB(spread, -spreadPower);
		var bandwidth = Math.ScaleB(0.9 * spread * Math.Pow(values.Count, -0.2), exponent + spreadPower);

		if (!double.IsFinite(bandwidth) || bandwidth <= 0.0)
		{
			throw new GGNetUserException("Automatic bandwidth cannot be represented as a positive finite double. Rescale the selected values or supply an explicit positive finite bandwidth.");
		}

		return bandwidth;
	}

	private static void ValidateDensityBandwidth(double? bandwidth)
	{
		if (bandwidth is double value && (!double.IsFinite(value) || value <= 0.0))
		{
			throw new GGNetUserException(FormattableString.Invariant($"bandwidth must be finite and strictly positive; received {value}. Supply a positive finite value or omit bandwidth to use automatic selection."));
		}
	}

	private static void ValidateDensityBound(double? bound, string name)
	{
		if (bound is double value && !double.IsFinite(value))
		{
			throw new GGNetUserException(FormattableString.Invariant($"{name} must be finite; received {value}. Supply a finite grid bound or omit it to use the sample range."));
		}
	}

	private static DensityGrid PrepareDensityGrid(List<double> values, double? bandwidth, int n, double? from, double? to)
	{
		var h = bandwidth ?? Nrd0(values);
		var lo = from ?? values.Min() - 3.0 * h;
		var hi = to ?? values.Max() + 3.0 * h;

		if (!double.IsFinite(lo))
		{
			lo = Math.FusedMultiplyAdd(-3.0, h, values.Min());
		}

		if (!double.IsFinite(hi))
		{
			hi = Math.FusedMultiplyAdd(3.0, h, values.Max());
		}

		ValidateDensityCoordinate(lo);
		ValidateDensityCoordinate(hi);

		var grid = new DensityGrid(values, h, n, lo, hi, (hi - lo) / (n - 1));

		for (var i = 0; i < n; i++)
		{
			ValidateDensityCoordinate(DensityCoordinate(grid, i));
		}

		return grid;
	}

	private static void ValidateDensityCoordinate(double value)
	{
		if (!double.IsFinite(value))
		{
			throw new GGNetUserException("Density grid coordinates cannot be represented as finite doubles. Supply finite from/to bounds, reduce bandwidth, or rescale the selected values.");
		}
	}

	private static double DensityCoordinate(DensityGrid grid, int index)
	{
		if (index == 0)
		{
			return grid.From;
		}

		if (index == grid.Count - 1)
		{
			return grid.To;
		}

		var at = grid.From + index * grid.Step;

		if (double.IsFinite(at))
		{
			return at;
		}

		var fraction = (double)index / (grid.Count - 1);

		return Math.Sign(grid.From) != Math.Sign(grid.To)
			? (1.0 - fraction) * grid.From + fraction * grid.To
			: grid.From + fraction * (grid.To - grid.From);
	}

	private static IEnumerable<(double at, double density)> Kde(DensityGrid grid)
	{
		var normalizer = grid.Values.Count * grid.Bandwidth * Math.Sqrt(2.0 * Math.PI);

		for (var i = 0; i < grid.Count; i++)
		{
			var at = DensityCoordinate(grid, i);
			var density = DensityAt(grid.Values, grid.Bandwidth, normalizer, at);

			if (!double.IsFinite(density) || density < 0.0)
			{
				throw new GGNetUserException("Density cannot be represented as a finite double. Increase bandwidth or rescale the selected values and grid bounds.");
			}

			yield return (at, density);
		}
	}

	private static double DensityAt(List<double> values, double h, double normalizer, double at)
	{
		if (double.IsNormal(normalizer))
		{
			var sum = 0.0;
			var differenceOverflow = false;
			var kernelUnderflow = false;

			for (var i = 0; i < values.Count; i++)
			{
				var difference = at - values[i];
				var u = difference / h;
				var kernel = Math.Exp(-0.5 * u * u);
				sum += kernel;
				differenceOverflow |= !double.IsFinite(difference);
				kernelUnderflow |= !double.IsNormal(kernel);
			}

			var density = sum / normalizer;

			if (double.IsNormal(density) && !differenceOverflow && !(kernelUnderflow && h < 1.0 && sum < Math.ScaleB(1.0, -990)))
			{
				return density >= Math.ScaleB(1.0, 1023) ? OverflowDensityAt(values, h, at) : density;
			}
		}

		return ScaledDensityAt(values, h, at);
	}

	private static double ScaledDensityAt(List<double> values, double h, double at)
	{
		var maximum = double.NegativeInfinity;

		for (var i = 0; i < values.Count; i++)
		{
			maximum = Math.Max(maximum, DensityExponent(at, values[i], h));
		}

		if (double.IsNegativeInfinity(maximum))
		{
			return 0.0;
		}

		var sum = 0.0;
		var compensation = 0.0;

		for (var i = 0; i < values.Count; i++)
		{
			var term = Math.Exp(DensityExponent(at, values[i], h) - maximum);
			var next = sum + term;
			compensation += sum >= term ? (sum - next) + term : (term - next) + sum;
			sum = next;
		}

		const double ln2High = 0.6931471805599453;
		const double ln2Low = 2.3190468138462996e-17;
		var kernelPower = (int)Math.Floor(maximum / ln2High);
		var remainder = Math.FusedMultiplyAdd(-kernelPower, ln2High, maximum) - kernelPower * ln2Low;
		var bandwidthPower = Math.ILogB(h);
		var bandwidthMantissa = Math.ScaleB(h, -bandwidthPower);
		var mantissa = (((sum + compensation) / values.Count) * Math.Exp(remainder)) / (bandwidthMantissa * Math.Sqrt(2.0 * Math.PI));

		var density = Math.ScaleB(mantissa, kernelPower - bandwidthPower);

		return density >= Math.ScaleB(1.0, 1023) ? OverflowDensityAt(values, h, at) : density;
	}

	private static double DensityExponent(double at, double value, double h)
	{
		var difference = at - value;
		var u = double.IsFinite(difference) ? difference / h : at / h - value / h;

		return Math.Abs(u) < 64.0 ? -0.5 * u * u : double.NegativeInfinity;
	}

	private readonly record struct DensityGrid(List<double> Values, double Bandwidth, int Count, double From, double To, double Step);

	private static (double value, int exponent) InterquartileRange(double[] sorted, bool scaled)
	{
		var h1 = (sorted.Length - 1) * 0.25;
		var h3 = (sorted.Length - 1) * 0.75;
		var lo1 = (int)h1;
		var lo3 = (int)h3;
		var a = sorted[lo1];
		var b = h1 == lo1 ? a : sorted[lo1 + 1];
		var c = sorted[lo3];
		var d = h3 == lo3 ? c : sorted[lo3 + 1];

		if (!scaled)
		{
			var iqr = (c + (h3 - lo3) * (d - c)) - (a + (h1 - lo1) * (b - a));

			if (double.IsNormal(iqr / 1.34) || a == d)
			{
				return (iqr, 0);
			}
		}

		var maximum = Math.Max(Math.Abs(a), Math.Abs(d));
		var exponent = maximum == 0.0 ? 0 : Math.ILogB(maximum);
		a = Math.ScaleB(a, -exponent);
		b = Math.ScaleB(b, -exponent);
		c = Math.ScaleB(c, -exponent);
		d = Math.ScaleB(d, -exponent);

		return ((c - a) + (h3 - lo3) * (d - c) - (h1 - lo1) * (b - a), exponent);
	}

	private static bool ScaledLessThan(double left, int leftExponent, double right, int rightExponent)
	{
		if (left == 0.0)
		{
			return true;
		}

		var leftPower = Math.ILogB(left);
		var rightPower = Math.ILogB(right);

		if (leftPower + leftExponent != rightPower + rightExponent)
		{
			return leftPower + leftExponent < rightPower + rightExponent;
		}

		return Math.ScaleB(left, -leftPower) < Math.ScaleB(right, -rightPower);
	}

	private static List<double> Values<T>(IReadOnlyList<T> source, Func<T, double> selector)
	{
		var values = new List<double>(source.Count);

		for (var i = 0; i < source.Count; i++)
		{
			values.Add(ValidateStatNumber(selector(source[i]), "Sample value"));
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

			values.Add(ValidateStatNumber(selector(source[i]), "Sample value"));
		}

		return groups;
	}
}
