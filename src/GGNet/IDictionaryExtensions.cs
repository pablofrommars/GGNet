namespace GGNet;

public static class IDictionaryExtensions
{
  public static TValue GetValueOr<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue @default)
  {
    if (dictionary.TryGetValue(key, out var value))
    {
      return value;
    }

    return @default;
  }
}
