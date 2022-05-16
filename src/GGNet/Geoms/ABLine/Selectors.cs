namespace GGNet.Geoms.ABLine;

internal sealed class Selectors<T>
{
	public Func<T, double> A { get; set; } = default!;

	public Func<T, double> B { get; set; } = default!;

	public Func<T, string>? Label { get; set; }
}