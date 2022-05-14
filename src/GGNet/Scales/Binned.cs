using GGNet.Common;
namespace GGNet.Scales;

public sealed class Binned<T> : Scale<double, T>
{
	private readonly Palettes.Binned<T> palette;
	private readonly T na;

	public Binned(Palettes.Binned<T> palette, T na = default!)
		: base()
	{
		this.palette = palette;
		this.na = na;
	}

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