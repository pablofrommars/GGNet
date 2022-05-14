namespace GGNet.Common;

public static class Convert<T>
{
	private static class ToDoubleHolder
	{
		internal delegate double Invoker(T value);

		private static Invoker Emit()
		{
			//if (!typeof(T).IsNumeric) throw

			var method = new DynamicMethod(string.Empty, typeof(double), new[] { typeof(T) });
			var il = method.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			if (typeof(T) != typeof(double))
			{
				il.Emit(OpCodes.Conv_R8);
			}
			il.Emit(OpCodes.Ret);

			return (Invoker)method.CreateDelegate(typeof(Invoker));
		}

		internal static Invoker Value = Emit();
	}

	public static double ToDouble(T value) => ToDoubleHolder.Value(value);
}