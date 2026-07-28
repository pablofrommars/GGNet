using GGNet.Exceptions;

namespace GGNet.Palettes;

public sealed class Discrete<TKey, TValue>(TValue[] palette, int direction = 1)
	where TKey : notnull
{
	private readonly TValue[] palette = Utils.NonEmpty(palette, "A discrete palette");
	private readonly int direction = direction;

	private int i;
	private readonly Dictionary<TKey, (int i, TValue value)> map = [];

	public TValue this[TKey key]
	{
		get => map[key].value;
	}

	public void Add(TKey key)
	{
		if (map.ContainsKey(key))
		{
			return;
		}

		// Two-phase: indices are handed out on Train; values land on Commit.
		map[key] = (i++, default!);
	}

	public void Set()
	{
		// Exhaustion is only knowable here: the key count comes from the data. Leaving the
		// mappings at default renders an empty chart with no diagnostic.
		var sub = Utils.Sample(palette, map.Count, direction)
			?? throw new GGNetUserException($"Palette exhausted: {map.Count} distinct keys were trained but the palette has only {palette.Length} value(s). Supply a palette with at least {map.Count} values.");

		foreach (var key in map.Keys.ToArray())
		{
			var entry = map[key];
			map[key] = (entry.i, sub[entry.i]);
		}
	}

	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
	{
		value = default;

		if (!map.TryGetValue(key, out var o))
		{
			return false;
		}

		value = o.value;

		return true;
	}

	public (TValue value, TKey key)[] Values()
	{
		var values = new (TValue value, TKey key)[map.Count];

		foreach (var (k, o) in map)
		{
			values[o.i] = (o.value, k);
		}

		return values;
	}

	public void Clear()
	{
		i = 0;
		map.Clear();
	}

	public static Discrete<TKey, TValue> New(TKey[] keys, TValue[] palette, int direction = 1)
	{
		var discrete = new Discrete<TKey, TValue>(palette, direction);

		for (var i = 0; i < keys.Length; i++)
		{
			discrete.Add(keys[i]);
		}

		discrete.Set();

		return discrete;
	}

	public static Discrete<TKey, TValue> Enum(TValue[] palette, int direction = 1) => New((TKey[])System.Enum.GetValues(typeof(TKey)), palette, direction);
}
