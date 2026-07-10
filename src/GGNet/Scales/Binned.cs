namespace GGNet.Scales;

// na is deliberately default/null: unmapped keys resolve to "no value" and the
// consuming geom skips the shape (fill checks string.IsNullOrEmpty and friends).
internal sealed class Binned<T>(Palettes.Binned<T> palette, T na = default!) : Scale<double, T>()
{
	private readonly Palettes.Binned<T> palette = palette;
	private readonly T na = na;

	public override Guide Guide => Guide.Items;

	public override void Train(double key) { }

	public override void Commit(bool grid) { }

	public override T Map(double key)
	{
		if (!palette.TryGetValue(key, out var result))
		{
			return na;
		}

		return result;
	}

	public override void Clear() { }
}
