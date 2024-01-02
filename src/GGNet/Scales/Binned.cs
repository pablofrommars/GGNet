namespace GGNet.Scales;

public sealed class Binned<T>(Palettes.Binned<T> palette, T na = default!) : Scale<double, T>()
{
	private readonly Palettes.Binned<T> palette = palette;
	private readonly T na = na;

    public override Guide Guide => Guide.Items;

	public override void Train(double key) { }

	public override void Set(bool grid) { }

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