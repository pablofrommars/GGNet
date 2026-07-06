namespace GGNet;

public static partial class Stat
{
	/// <summary>
	/// Counts items per distinct key, in key order: a source for
	/// <c>Geom_Bar</c> over categorical data (x: key, y: n).
	/// </summary>
	/// <param name="source">Items to count; recomputed every render pass.</param>
	/// <param name="selector">Key per item.</param>
	public static StatSource<Count<TKey>> Count<T, TKey>(IReadOnlyList<T> source, Func<T, TKey> selector)
		where TKey : notnull
		=> new(() =>
		{
			var counts = new Dictionary<TKey, int>();

			for (var i = 0; i < source.Count; i++)
			{
				var key = selector(source[i]);

				counts[key] = counts.GetValueOrDefault(key) + 1;
			}

			var result = new List<Count<TKey>>();

			foreach (var key in counts.Keys.Order())
			{
				result.Add(new(key, counts[key]));
			}

			return result;
		});
}
