namespace GGNet.Geoms.Ribbon;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; set; }

	public Func<T, TY>? YMin { get; set; }

	public Func<T, TY>? YMax { get; set; }

	public Func<T, RenderFragment>? Tooltip { get; set; }
}
