using GGNet.Scales;

namespace GGNet.Readings;

internal static class InspectGroup
{
	public static Func<T, string?>? Of<T, TValue>(IAestheticMapping<T, TValue>? mapping)
		where TValue : notnull
	{
		if (mapping is null)
		{
			return null;
		}

		Dictionary<TValue, string> labels = [];

		foreach (var (value, label) in mapping.Labels)
		{
			labels.TryAdd(value, label);
		}

		return item => mapping.Map(item) is { } value && !(value is string text && text.Length == 0)
			? labels.GetValueOrDefault(value) ?? Convert.ToString(value, CultureInfo.InvariantCulture)
			: null;
	}

	public static Func<T, string?>? Of<T>(Func<T, string?>? first, Func<T, string?>? second)
	{
		if (first is null || second is null)
		{
			return first ?? second;
		}

		return item => first(item) is { } a && second(item) is { } b ? a + ", " + b : null;
	}
}
