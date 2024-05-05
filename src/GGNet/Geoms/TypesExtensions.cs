namespace GGNet.Geoms;

public static class TypesExtensions
{
	public static bool IsNumeric(this Type type) =>
		type == typeof(double) ||
		type == typeof(int) ||
		type == typeof(float) ||
		type == typeof(uint) ||
		type == typeof(long) ||
		type == typeof(ulong) ||
		type == typeof(short) ||
		type == typeof(ushort) ||
		type == typeof(byte);
}
