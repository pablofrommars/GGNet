using GGNet.Transformations;

namespace GGNet.Scales;

internal sealed class ColorDiscrete<TKey> : Discrete<TKey, string>
	where TKey : notnull
{
	public ColorDiscrete(
	   Palettes.Discrete<TKey, string> palette,
		ITransformation<TKey>? transformation = null)
	   // na: deliberately null — unmapped keys yield no color and the geom
	   // skips the shape (string.IsNullOrEmpty checks at every fill/color site).
	   : base(palette, default!, transformation)
	{
	}

	public ColorDiscrete(
		string[] palette,
		int direction = 1,
		ITransformation<TKey>? transformation = null)
		: base(palette, direction, default!, transformation)
	{
	}

	public override Guide Guide => Guide.Items;
}
