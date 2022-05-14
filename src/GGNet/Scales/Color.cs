using GGNet.Common;
using GGNet.Transformations;

namespace GGNet.Scales;

public sealed class ColorDiscrete<TKey> : Discrete<TKey, string>
	where TKey : notnull
{
	public ColorDiscrete(
	   Palettes.Discrete<TKey, string> palette,
		ITransformation<TKey>? transformation = null)
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