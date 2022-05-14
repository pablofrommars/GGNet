namespace GGNet.Data;

internal sealed class Positions<TX, TY>
	where TX : struct
	where TY : struct
{
	public Position<TX> X { get; } = new();

	public Position<TY> Y { get; } = new();
}