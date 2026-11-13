namespace GGNet.Readings;

internal sealed class SeriesIndex<T>(Inspect<T> inspect, string? group, double? limit, double halfExtent, double[] coordinates, double[] extents, Func<double, double>? mapCursor, T[] items, Func<T, (double x, double y)> position) : ISeriesIndex
{
	public string Name { get; } = group is null ? inspect.Name : inspect.GroupName?.Invoke(group) ?? group;

	public int Count => coordinates.Length;

	public double Coordinate(int row) => coordinates[row];

	public (double x, double y) Position(int row) => position(items[row]);

	public static IReadOnlyList<ISeriesIndex> Build(
		Inspect<T> inspect,
		IEnumerable<T> source,
		Func<T, double> coordinate,
		Func<T, bool> hasValue,
		Func<T, string?>? grouping,
		Func<T, (double x, double y)> position,
		double? limit,
		double? barWidth = null,
		Func<T, double>? extent = null,
		Func<double, double>? mapCursor = null)
	{
		List<string?> order = [];
		Dictionary<string, List<(double x, T item)>> groups = [];
		List<(double x, T item)>? single = grouping is null ? [] : null;

		foreach (var item in source)
		{
			var x = coordinate(item);
			var valid = double.IsFinite(x) && hasValue(item);

			if (single is not null)
			{
				if (valid)
				{
					single.Add((x, item));
				}

				continue;
			}

			if (grouping!(item) is not { } key)
			{
				continue;
			}

			if (!groups.TryGetValue(key, out var rows))
			{
				rows = [];
				groups[key] = rows;
				order.Add(key);
			}

			if (valid)
			{
				rows.Add((x, item));
			}
		}

		if (single is not null)
		{
			order.Add(null);
		}

		var drawn = extent ?? coordinate;
		var halfExtent = barWidth is { } width
			? width * Spacing(single is not null ? [single] : groups.Values, drawn) / 2.0
			: 0.0;

		var series = new ISeriesIndex[order.Count];

		for (var i = 0; i < series.Length; i++)
		{
			var rows = order[i] is { } key ? groups[key] : single!;
			var sorted = rows.Select((row, position) => (row.x, position, row.item))
				.OrderBy(row => row.x)
				.ThenBy(row => row.position)
				.ToArray();

			series[i] = new SeriesIndex<T>(inspect, order[i], limit, halfExtent, [.. sorted.Select(row => row.x)], [.. sorted.Select(row => drawn(row.item))], mapCursor, [.. sorted.Select(row => row.item)], position);
		}

		return series;
	}

	private static double Spacing(IEnumerable<List<(double x, T item)>> groups, Func<T, double> drawn)
	{
		var distinct = groups.SelectMany(rows => rows.Select(row => drawn(row.item))).Distinct().Order().ToArray();

		if (distinct.Length < 2)
		{
			return 1.0;
		}

		var spacing = double.MaxValue;

		for (var i = 1; i < distinct.Length; i++)
		{
			spacing = Math.Min(spacing, distinct[i] - distinct[i - 1]);
		}

		return spacing;
	}

	public SeriesReading Read(double cursor)
	{
		if (!double.IsFinite(cursor) || coordinates.Length == 0)
		{
			return Missing();
		}

		var after = UpperBound(cursor);
		var before = after - 1;

		switch (inspect.Policy)
		{
			case InspectPolicy.Nearest:
				{
					var row = Closest(coordinates, cursor, before, after);

					return limit is { } max && Math.Abs(coordinates[row] - cursor) > max ? Missing() : Found(row, ReadingKind.Observed);
				}

			case InspectPolicy.Previous:
				return before < 0 || (limit is { } age && cursor - coordinates[before] > age) ? Missing() : Found(before, ReadingKind.Previous);

			case InspectPolicy.Step:
				return before < 0 ? Missing() : Found(before, ReadingKind.Step);

			case InspectPolicy.Containing:
				{
					var at = mapCursor?.Invoke(cursor) ?? cursor;

					if (!double.IsFinite(at))
					{
						return Missing();
					}

					var row = Closest(extents, at, before, after);

					return Math.Abs(extents[row] - at) > halfExtent ? Missing() : Found(row, ReadingKind.Containing);
				}

			default:
				throw new Exceptions.GGNetInternalException($"Unmapped {nameof(InspectPolicy)}: {inspect.Policy}");
		}
	}

	public double? After(double cursor)
	{
		var index = UpperBound(cursor);

		return index < coordinates.Length ? coordinates[index] : null;
	}

	public double? Before(double cursor)
	{
		var index = UpperBound(cursor) - 1;

		while (index >= 0 && coordinates[index] >= cursor)
		{
			index--;
		}

		return index >= 0 ? coordinates[index] : null;
	}

	private static int Closest(double[] positions, double cursor, int before, int after)
	{
		if (before < 0)
		{
			return after;
		}

		if (after >= positions.Length)
		{
			return before;
		}

		return cursor - positions[before] <= positions[after] - cursor ? before : after;
	}

	private int UpperBound(double cursor)
	{
		var low = 0;
		var high = coordinates.Length;

		while (low < high)
		{
			var middle = low + (high - low) / 2;

			if (coordinates[middle] <= cursor)
			{
				low = middle + 1;
			}
			else
			{
				high = middle;
			}
		}

		return low;
	}

	private SeriesReading Found(int row, ReadingKind kind)
		=> new(inspect.Name, group, Name, kind, coordinates[row], inspect.Value(items[row]), row);

	private SeriesReading Missing()
		=> new(inspect.Name, group, Name, ReadingKind.Missing, null, inspect.Missing, -1);
}
