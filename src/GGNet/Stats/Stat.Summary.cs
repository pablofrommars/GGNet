namespace GGNet;

public static partial class Stat
{
	/// <summary>
	/// Mean and ±spread·sd per distinct x, in x order: a source for
	/// <c>Geom_ErrorBar</c> (x, y: center, ymin: lower, ymax: upper).
	/// Nonfinite numeric inputs and unrepresentable interval endpoints throw
	/// <see cref="Exceptions.GGNetUserException"/> at calculation or recomputation.
	/// </summary>
	/// <param name="source">Items to summarize; recomputed every render pass.</param>
	/// <param name="x">Finite position key per item, in x-axis data units.</param>
	/// <param name="y">Finite value per item, in y-axis data units.</param>
	/// <param name="spread">Finite half-width of the interval in sample standard deviations. Zero produces the mean alone; negative values reverse the endpoints. Unrepresentable finite endpoints are rejected.</param>
	public static StatSource<Summary> Summary<T>(IReadOnlyList<T> source, Func<T, double> x, Func<T, double> y, double spread = 1.0)
		=> new(source as IStatSource, () =>
		{
			ValidateStatNumber(spread, "Summary spread");
			var groups = Group(source, y, item => ValidateStatNumber(x(item), "Summary x"));

			var result = new List<Summary>();

			foreach (var key in groups.Keys.Order())
			{
				var (center, lower, upper) = SummaryInterval(groups[key], spread);

				result.Add(new(key, center, lower, upper));
			}

			return result;
		});

	/// <summary>
	/// Mean and ±spread·sd per (group, x): a faceted summary source. Facet
	/// the output on the same key.
	/// Nonfinite numeric inputs and unrepresentable interval endpoints throw
	/// <see cref="Exceptions.GGNetUserException"/> at calculation or recomputation.
	/// </summary>
	/// <param name="source">Items to summarize; recomputed every render pass.</param>
	/// <param name="x">Finite position key per item, in x-axis data units.</param>
	/// <param name="y">Finite value per item, in y-axis data units.</param>
	/// <param name="groupBy">Group key per item; summarized independently within each group.</param>
	/// <param name="spread">Finite half-width of the interval in sample standard deviations. Zero produces the mean alone; negative values reverse the endpoints. Unrepresentable finite endpoints are rejected.</param>
	public static StatSource<Summary<TKey>> Summary<T, TKey>(IReadOnlyList<T> source, Func<T, double> x, Func<T, double> y, Func<T, TKey> groupBy, double spread = 1.0)
		where TKey : notnull
		=> new(source as IStatSource, () =>
		{
			ValidateStatNumber(spread, "Summary spread");
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

				var xk = ValidateStatNumber(x(item), "Summary x");

				if (!byX.TryGetValue(xk, out var values))
				{
					values = [];
					byX[xk] = values;
				}

				values.Add(ValidateStatNumber(y(item), "Sample value"));
			}

			var result = new List<Summary<TKey>>();

			foreach (var key in groups.Keys.Order())
			{
				var byX = groups[key];

				foreach (var xk in byX.Keys.Order())
				{
					var (center, lower, upper) = SummaryInterval(byX[xk], spread);

					result.Add(new(key, xk, center, lower, upper));
				}
			}

			return result;
		});

}
