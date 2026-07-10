namespace GGNet;

public static partial class Stat
{
	/// <summary>
	/// Mean and ±spread·sd per distinct x, in x order: a source for
	/// <c>Geom_ErrorBar</c> (x, y: center, ymin: lower, ymax: upper).
	/// </summary>
	/// <param name="source">Items to summarize; recomputed every render pass.</param>
	/// <param name="x">Position key per item, in x-axis data units.</param>
	/// <param name="y">Value per item, in y-axis data units.</param>
	/// <param name="spread">Half-width of the interval in sample standard deviations.</param>
	public static StatSource<Summary> Summary<T>(IReadOnlyList<T> source, Func<T, double> x, Func<T, double> y, double spread = 1.0)
		=> new(() =>
		{
			var groups = Group(source, y, x);

			var result = new List<Summary>();

			foreach (var key in groups.Keys.Order())
			{
				var (center, sd) = MeanSd(groups[key]);

				result.Add(new(key, center, center - spread * sd, center + spread * sd));
			}

			return result;
		});

	/// <summary>
	/// Mean and ±spread·sd per (group, x): a faceted summary source. Facet
	/// the output on the same key.
	/// </summary>
	/// <param name="source">Items to summarize; recomputed every render pass.</param>
	/// <param name="x">Position key per item, in x-axis data units.</param>
	/// <param name="y">Value per item, in y-axis data units.</param>
	/// <param name="groupBy">Group key per item; summarized independently within each group.</param>
	/// <param name="spread">Half-width of the interval in sample standard deviations.</param>
	public static StatSource<Summary<TKey>> Summary<T, TKey>(IReadOnlyList<T> source, Func<T, double> x, Func<T, double> y, Func<T, TKey> groupBy, double spread = 1.0)
		where TKey : notnull
		=> new(() =>
		{
			var groups = new Dictionary<TKey, Dictionary<double, List<double>>>();

			for (var i = 0; i < source.Count; i++)
			{
				var item = source[i];
				var key = groupBy(item);

				if (!groups.TryGetValue(key, out var byX))
				{
					byX = [];
					groups[key] = byX;
				}

				var xk = x(item);

				if (!byX.TryGetValue(xk, out var values))
				{
					values = [];
					byX[xk] = values;
				}

				values.Add(y(item));
			}

			var result = new List<Summary<TKey>>();

			foreach (var key in groups.Keys.Order())
			{
				var byX = groups[key];

				foreach (var xk in byX.Keys.Order())
				{
					var (center, sd) = MeanSd(byX[xk]);

					result.Add(new(key, xk, center, center - spread * sd, center + spread * sd));
				}
			}

			return result;
		});

	private static (double mean, double sd) MeanSd(List<double> values)
	{
		var mean = 0.0;

		for (var i = 0; i < values.Count; i++)
		{
			mean += values[i];
		}

		mean /= values.Count;

		if (values.Count < 2)
		{
			return (mean, 0.0);
		}

		var ss = 0.0;

		for (var i = 0; i < values.Count; i++)
		{
			var d = values[i] - mean;

			ss += d * d;
		}

		return (mean, Math.Sqrt(ss / (values.Count - 1)));
	}
}
