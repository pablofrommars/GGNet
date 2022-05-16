namespace GGNet.Geoms.Text;

internal sealed class Selectors<T, TX, TY, TT>
{
	public Func<T, TX>? X { get; set; }

	public Func<T, TY>? Y { get; set; }

	public Func<T, double>? Angle { get; set; }

	public Func<T, TT>? Text { get; set; }
}