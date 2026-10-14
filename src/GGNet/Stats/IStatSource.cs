namespace GGNet;

/// <summary>
/// A source whose items are computed from other data. The render pipeline recomputes each distinct
/// stat source once per pass, so streaming data re-bins on every refresh.
/// </summary>
public interface IStatSource
{
	void Recompute();
}
