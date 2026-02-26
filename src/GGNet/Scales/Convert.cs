using System.Numerics;

namespace GGNet.Scales;

public static class Convert<T> where T : struct
{
	// JIT elides boxing and dead branches for each value-type specialization
	public static double ToDouble(T value)
	{
		if (typeof(T) == typeof(double)) return (double)(object)value;
		if (typeof(T) == typeof(int)) return (int)(object)value;
		if (typeof(T) == typeof(float)) return (float)(object)value;
		if (typeof(T) == typeof(long)) return (long)(object)value;
		if (typeof(T) == typeof(short)) return (short)(object)value;
		if (typeof(T) == typeof(byte)) return (byte)(object)value;
		if (typeof(T) == typeof(uint)) return (uint)(object)value;
		if (typeof(T) == typeof(ushort)) return (ushort)(object)value;
		if (typeof(T) == typeof(ulong)) return (ulong)(object)value;

		throw new NotSupportedException($"Cannot convert {typeof(T)} to double");
	}
}
