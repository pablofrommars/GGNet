using static System.Math;

namespace GGNet.Palettes;

public sealed class Continuous<T>(T[] values)
{
	private readonly T[] values = values;

    public T Get(double v) => values[(int)(Max(Min(v, 1), 0) * (values.Length - 1))];
}