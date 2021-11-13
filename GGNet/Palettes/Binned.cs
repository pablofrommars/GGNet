namespace GGNet.Palettes;

public class Binned<T>
{
	private readonly double[] breaks;
	private readonly T[] values;

	public Binned(double[] breaks, T[] values)
	{
		//Valid: breaks.Length >= 2 && breaks.Lenght == (values.Lenght + 1)
		this.breaks = breaks;
		this.values = values;
	}

	public bool TryGetValue(double key, out T value)
	{
		value = default;

		if (key < breaks[0] || key > breaks[^1])
		{
			return false;
		}

		for (int i = 0; i < values.Length; i++)
		{
			if (breaks[i] <= key && key < breaks[i + 1])
			{
				value = values[i];
				return true;
			}
		}

		return false;
	}
}