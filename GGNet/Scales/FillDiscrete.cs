using GGNet.Transformations;

namespace GGNet.Scales;

public class FillDiscrete<TKey> : Discrete<TKey, string>
	where TKey : notnull
{
	public FillDiscrete(
	   Palettes.Discrete<TKey, string> palette,
		ITransformation<TKey>? transformation = null)
	   : base(palette, default!, transformation)
	{
	}

	public FillDiscrete(
		string[] palette,
		int direction = 1,
		ITransformation<TKey>? transformation = null)
		: base(palette, direction, default!, transformation)
	{
	}

	public override Guide Guide => Guide.Items;
}
