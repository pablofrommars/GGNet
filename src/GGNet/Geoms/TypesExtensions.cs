namespace GGNet.Geoms;

public static class TypesExtensions
{
	private static readonly FrozenSet<Type> NumericTypes = new HashSet<Type>
	{
		typeof(double),
		typeof(int),
		typeof(float),
		typeof(uint),
		typeof(long),
		typeof(ulong),
		typeof(short),
		typeof(ushort),
		typeof(byte)
	}.ToFrozenSet();

	public static bool IsNumeric(this Type type) => NumericTypes.Contains(type);
}
