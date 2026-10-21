namespace GGNet;

/// <summary>
/// Statistical sources: each factory computes its initial items eagerly. Rendering refreshes directly
/// composed built-in sources upstream first, once per source identity, before any geom draws them.
/// Per-facet statistics are grouped statistics — compute with <c>groupBy</c> and facet the output
/// on the same key.
/// </summary>
public static partial class Stat
{
	/// <summary>
	/// Bins values into approximately equal-width intervals over the data range: a histogram
	/// source for <c>Geom_Bar</c> (x: mid, y: count).
	/// </summary>
	/// <remarks>
	/// Intervals share finite, strictly increasing edges, include their lower bound, and exclude
	/// their upper bound except in the final interval. Nonconstant ranges retain exact endpoints.
	/// Constant data tries unit-width padding above, then below the value. If neither works, padding
	/// uses twice the local floating-point spacing (at least one), again trying above then below.
	/// Nominal rounded subdivisions must remain distinct; uneven redistribution is not performed.
	/// Nonfinite values, collapsed requested intervals, and unrepresentable positive densities throw
	/// <see cref="Exceptions.GGNetUserException"/>. Use fewer bins or rescale the selected values.
	/// Empty input or fewer than one requested bin returns an empty source.
	/// </remarks>
	/// <param name="source">Items to bin; recomputed every render pass.</param>
	/// <param name="selector">Value per item, in x-axis data units.</param>
	/// <param name="bins">Number of approximately equal-width bins over the data range (per group when grouped).</param>
	public static StatSource<Bin> Bin<T>(IReadOnlyList<T> source, Func<T, double> selector, int bins = 30)
		=> new(source as IStatSource, () => ComputeBins(source, selector, bins));

	/// <summary>
	/// Bins values per group, each group over its own range: a faceted or
	/// fill-split histogram source. Facet the output on the same key:
	/// <c>Facet_Wrap(b =&gt; b.Group)</c>.
	/// </summary>
	/// <remarks>
	/// Each group follows the ungrouped histogram's interval, padding, and numeric validation contract.
	/// An invalid group fails the whole computation, including during refresh.
	/// </remarks>
	/// <param name="source">Items to bin; recomputed every render pass.</param>
	/// <param name="selector">Value per item, in x-axis data units.</param>
	/// <param name="groupBy">Group key per item; binning runs independently within each group.</param>
	/// <param name="bins">Number of approximately equal-width bins over the data range (per group when grouped).</param>
	public static StatSource<Bin<TKey>> Bin<T, TKey>(IReadOnlyList<T> source, Func<T, double> selector, Func<T, TKey> groupBy, int bins = 30)
		where TKey : notnull
		=> new(source as IStatSource, () =>
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

			var result = new List<Bin<TKey>>();

			foreach (var key in groups.Keys.Order())
			{
				foreach (var bin in ComputeBins(groups[key], v => v, bins))
				{
					result.Add(new(key, bin.Min, bin.Mid, bin.Max, bin.Count, bin.Density));
				}
			}

			return result;
		});

	private static List<Bin> ComputeBins<T>(IReadOnlyList<T> source, Func<T, double> selector, int bins)
	{
		var result = new List<Bin>();

		if (source.Count == 0 || bins < 1)
		{
			return result;
		}

		var min = double.MaxValue;
		var max = double.MinValue;
		var values = new double[source.Count];

		for (var i = 0; i < source.Count; i++)
		{
			var v = selector(source[i]);

			if (!double.IsFinite(v))
			{
				throw new Exceptions.GGNetUserException("Stat.Bin requires finite selected values. Remove NaN and infinity or correct the selector before binning.");
			}

			values[i] = v;

			min = Math.Min(min, v);
			max = Math.Max(max, v);
		}

		var edges = HistogramEdges(min, max, bins);
		var counts = new int[bins];

		foreach (var value in values)
		{
			var index = Array.BinarySearch(edges, value);

			if (index < 0)
			{
				index = ~index - 1;
			}

			counts[index == bins ? bins - 1 : index]++;
		}

		var total = (double)source.Count;
		var nominalWidth = (max - min) / bins;

		for (var b = 0; b < bins; b++)
		{
			var lo = edges[b];
			var hi = edges[b + 1];
			var width = hi - lo;
			var mid = double.IsFinite(width) ? lo + width / 2 : lo / 2 + hi / 2;

			if (double.IsSubnormal(width))
			{
				mid = (lo + hi) / 2;
			}
			else if (double.IsNormal(nominalWidth))
			{
				var nominalMid = lo + nominalWidth / 2;

				if (double.IsFinite(nominalMid) && nominalMid >= lo && nominalMid <= hi)
				{
					mid = nominalMid;
				}
			}

			var mass = counts[b] / total;
			var density = double.IsFinite(width) ? mass / width : (mass / 2) / (hi / 2 - lo / 2);

			if (!double.IsFinite(density) || (counts[b] > 0 && density == 0))
			{
				throw new Exceptions.GGNetUserException("Stat.Bin cannot represent a positive bin density as a finite double. Rescale the selected values or use fewer bins.");
			}

			result.Add(new(lo, mid, hi, counts[b], density));
		}

		return result;
	}
}
