namespace GGNet.Geoms.Bar;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; set; }

	public Func<T, TY>? Y { get; set; }

	public Func<T, string>? Tooltip { get; set; }
}