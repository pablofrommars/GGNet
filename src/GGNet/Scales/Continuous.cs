using GGNet.Transformations;

namespace GGNet.Scales;

internal abstract class Continuous<TKey>(ITransformation<TKey>? transformation) : Scale<TKey, double>(transformation)
	where TKey : struct
{
	public (TKey? min, TKey? max) Limits { get; set; }
}
