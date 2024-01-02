namespace GGNet.Geoms.HLine;

internal sealed record Selectors<T, TY>
{
	public required Func<T, TY> Y { get; init; }

	public required Func<T, string> Label { get; init; }
}