using GGNet.Common;

namespace GGNet.Elements;

public sealed record Margin
{
	public double Top { get; init; }

	public double Right { get; init; }

	public double Bottom { get; init; }

	public double Left { get; init; }

	public Units Units { get; init; } = Units.px;
}