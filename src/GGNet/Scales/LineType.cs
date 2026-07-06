using GGNet.Transformations;

namespace GGNet.Scales;

internal sealed class LineTypeDiscrete<TKey> : Discrete<TKey, LineType>
  where TKey : notnull
{
	private static readonly LineType[] lineTypes = Enum.GetValues<LineType>();

	public LineTypeDiscrete(
	  Palettes.Discrete<TKey, LineType> palette,
		 ITransformation<TKey>? transformation = null)
		  : base(palette, default, transformation)
	{
	}

	public LineTypeDiscrete(
	  LineType[]? palette = null,
	  int direction = 1,
	  ITransformation<TKey>? transformation = null)
		: base(palette ?? lineTypes, direction, default, transformation)
	{
	}

	public override Guide Guide => Guide.None;
}
