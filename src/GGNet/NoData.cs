namespace GGNet;

/// <summary>
/// Phantom source type for plots built without data (<c>Build()</c> with no source): annotation-only
/// plots, or plots where every geom supplies its own source. Never instantiated.
/// </summary>
public sealed class NoData
{
	private NoData() { }
}
