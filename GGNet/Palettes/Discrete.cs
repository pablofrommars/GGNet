namespace GGNet.Palettes;

public class Discrete<TKey, TValue>
    where TKey : notnull
{
	private readonly TValue[] palette;
	private readonly int direction;

	private int i = 0;
	private readonly Dictionary<TKey, (int i, TValue value)> map = new();

	public Discrete(TValue[] palette, int direction = 1)
	{
		this.palette = palette;
		this.direction = direction;
	}

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

		map[key] = (i++, default!);
	}

	public void Set()
	{
		var sub = Utils.Sample(palette, map.Count, direction);
		if (sub is null)
		{
			return;
		}

		foreach (var (key, i) in map.Select(o => (o.Key, o.Value.i)).ToList())
		{
			map[key] = (i, sub[i]);
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