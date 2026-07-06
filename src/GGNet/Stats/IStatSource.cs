namespace GGNet;

// A source whose items are computed from other data. The render pipeline's
// Reset() recomputes each distinct stat source once per pass, so streaming
// data re-bins on every refresh — stats are per-pass state like everything
// else the pipeline owns.
public interface IStatSource
{
	void Recompute();
}
