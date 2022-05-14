namespace GGNet.Data;

internal sealed class Dimension<TValue>
{
	public TValue Value { get; set; } = default!;

	public double Width { get; set; }

	public double Height { get; set; }
}