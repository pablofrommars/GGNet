namespace GGNet.Geoms.VLine;

internal sealed class Selectors<T, TX>
{
	public Func<T, TX> X { get; set; } = default!;

	public Func<T, string> Label { get; set; } = default!;
}