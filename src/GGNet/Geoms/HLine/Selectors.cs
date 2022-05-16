namespace GGNet.Geoms.HLine;

internal sealed class Selectors<T, TY>
{
	public Func<T, TY> Y { get; set; } = default!;

	public Func<T, string> Label { get; set; } = default!;
}