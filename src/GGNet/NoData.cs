namespace GGNet;

// Phantom source type for plots built without data (Build() with no source):
// annotation-only plots, or plots where every geom supplies its own source.
// Never instantiated.
public sealed class NoData
{
	private NoData() { }
}
