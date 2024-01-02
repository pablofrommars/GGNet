namespace GGNet.Data;

internal sealed record Dimension<TValue>
{
	public required TValue Value { get; set; }

	public double Width { get; set; }

	public double Height { get; set; }
}