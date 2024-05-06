namespace GGNet.Geoms.Hex;

internal sealed record Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; init; }

	public Func<T, TY>? Y { get; init; }

	public required Func<T, TX> Dx { get; init; }

	public required Func<T, TY> Dy { get; init; }

	public Func<T, RenderFragment>? Tooltip { get; init; }
}
