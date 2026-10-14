using GGNet.Transformations;

namespace GGNet.Scales;

internal abstract class Discrete<TKey, TValue> : Scale<TKey, TValue>
	where TKey : notnull
{
	private readonly bool defined;
	private readonly Palettes.Discrete<TKey, TValue> discretePalette;
	private readonly TValue naValue;

	public Discrete(
		Palettes.Discrete<TKey, TValue> palette,
		TValue na,
		ITransformation<TKey>? transformation)
			: base(transformation)
	{
		defined = true;
		discretePalette = palette;
		naValue = na;
	}

	public Discrete(
		TValue[] palette,
		int direction,
		TValue na,
		ITransformation<TKey>? transformation)
			: base(transformation)
	{
		discretePalette = new Palettes.Discrete<TKey, TValue>(palette, direction);
		naValue = na;
	}

	public override void Train(TKey key)
	{
		if (defined)
		{
			return;
		}

		discretePalette.Add(key);
	}

	public override void Commit(bool grid)
	{
		if (!defined)
		{
			discretePalette.Set();
		}

		if (!grid)
		{
			return;
		}

		var values = discretePalette.Values();

		var breaks = new TValue[values.Length];
		var labels = new (TValue value, string label)[values.Length];

		for (int i = 0; i < values.Length; i++)
		{
			breaks[i] = values[i].value;
			labels[i] = (values[i].value, Formats.InvariantText.Of(values[i].key)!);
		}

		Breaks = breaks;
		Labels = labels;
	}

	public override TValue Map(TKey key)
	{
		if (!discretePalette.TryGetValue(key, out var result))
		{
			return naValue;
		}

		return result;
	}

	public override void Clear()
	{
		if (!defined)
		{
			discretePalette.Clear();
		}
	}
}
