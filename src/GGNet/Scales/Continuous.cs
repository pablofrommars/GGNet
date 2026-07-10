using GGNet.Transformations;

namespace GGNet.Scales;

internal abstract class Continuous<TKey>(ITransformation<TKey>? transformation) : Scale<TKey, double>(transformation)
	where TKey : struct
{
	// Spec bucket: the author's build-time window (XLim/YLim). Clear() must
	// never touch it — Commit re-reads it every pass.
	public (TKey? min, TKey? max) Limits { get; set; }
}
