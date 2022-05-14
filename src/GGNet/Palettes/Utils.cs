namespace GGNet.Palettes;

public static class Utils
{
	public static T[]? Sample<T>(T[] palette, int n, int direction = 1)
	{
		if (n > palette.Length)
		{
			return null;
		}

		var values = new T[n];

		if (n == 1)
		{
			values[0] = direction >= 0 ? palette[0] : palette[^1];

			return values;
		}

		var delta = (palette.Length - 1.0) / (n - 1.0);

		for (int i = 0; i < n; i++)
		{
			values[direction >= 0 ? i : n - i - 1] = palette[(int)(i * delta)];
		}

		return values;
	}
}