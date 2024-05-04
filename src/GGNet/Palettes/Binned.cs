namespace GGNet.Palettes;

public sealed class Binned<T>(double[] breaks, T[] values)
{
  private readonly double[] breaks = breaks;
  private readonly T[] values = values;

  public bool TryGetValue(double key, [MaybeNullWhen(false)] out T value)
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
