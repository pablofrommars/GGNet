namespace GGNet;

// Stats are sources, not layers: each method returns a typed, lazily
// recomputed source that any geom draws unchanged. Per-facet statistics are
// grouped statistics — compute with groupBy and facet the output on the same
// key (Facet_Wrap(b => b.Group)); the key is deliberately stated twice.
public static partial class Stat
{
	/// <summary>
	/// Bins values into equal-width intervals over the data range: a histogram
	/// source for <c>Geom_Bar</c> (x: mid, y: count).
	/// </summary>
	/// <param name="source">Items to bin; recomputed every render pass.</param>
	/// <param name="selector">Value per item, in x-axis data units.</param>
	/// <param name="bins">Number of equal-width bins over the data range (per group when grouped).</param>
	public static StatSource<Bin> Bin<T>(IReadOnlyList<T> source, Func<T, double> selector, int bins = 30)
		=> new(() => ComputeBins(source, selector, bins));

	/// <summary>
	/// Bins values per group, each group over its own range: a faceted or
	/// fill-split histogram source. Facet the output on the same key:
	/// <c>Facet_Wrap(b =&gt; b.Group)</c>.
	/// </summary>
	/// <param name="source">Items to bin; recomputed every render pass.</param>
	/// <param name="selector">Value per item, in x-axis data units.</param>
	/// <param name="groupBy">Group key per item; binning runs independently within each group.</param>
	/// <param name="bins">Number of equal-width bins over the data range (per group when grouped).</param>
	public static StatSource<Bin<TKey>> Bin<T, TKey>(IReadOnlyList<T> source, Func<T, double> selector, Func<T, TKey> groupBy, int bins = 30)
		where TKey : notnull
		=> new(() =>
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

		for (var i = 0; i < source.Count; i++)
		{
			var v = selector(source[i]);

			min = Math.Min(min, v);
			max = Math.Max(max, v);
		}

		// Degenerate range: one bin of nominal width centered on the value.
		var width = max > min ? (max - min) / bins : 1.0;

		var counts = new int[bins];

		for (var i = 0; i < source.Count; i++)
		{
			var v = selector(source[i]);

			var index = max > min ? (int)((v - min) / width) : 0;

			if (index == bins)
			{
				index--;
			}

			counts[index]++;
		}

		var total = (double)source.Count;

		for (var b = 0; b < bins; b++)
		{
			var lo = min + b * width;

			result.Add(new(lo, lo + width / 2.0, lo + width, counts[b], counts[b] / (total * width)));
		}

		return result;
	}
}
