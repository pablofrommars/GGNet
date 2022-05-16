namespace GGNet.Geoms.Hex;

internal sealed class Selectors<T, TX, TY>
{
	public Func<T, TX>? X { get; init; }

	public Func<T, TY>? Y { get; init; }

	public Func<T, TX> Dx { get; init; } = default!;

	public Func<T, TY> Dy { get; init; } = default!;

	public Func<T, string>? Tooltip { get; init; }
}