namespace GGNet.Formats;

public sealed class Labeller<T>(Func<T, string> selector) : IFormatter<T>
{
	private readonly Func<T, string> selector = selector;

  public string Format(T value) => selector(value);
}
