using GGNet.Exceptions;

namespace GGNet.Scales;

internal static class PositionRange
{
	// TODO: calibration — widen tick-search bounds only after extreme-range tests establish finite scores and bounded search.
	private const double minimumTickSearchSpan = 1e-140;
	private const double maximumTickSearchSpan = 1e140;
	private const double maximumTickSearchOffset = 1e6;

	public static void Validate(double min, double max)
	{
		if (!double.IsFinite(min) || !double.IsFinite(max))
		{
			throw new GGNetUserException("Position bounds must be finite; check the data, scale limits, expansion, and view window.");
		}
	}

	public static (double min, double max) Expand(double min, double max,
		(double minMult, double minAdd, double maxMult, double maxAdd) expand)
	{
		Validate(min, max);
		Validate(expand.minMult, expand.minAdd);
		Validate(expand.maxMult, expand.maxAdd);

		if (max < min)
		{
			(min, max) = (max, min);
		}

		double lower;
		double upper;
		if (min == max)
		{
			var pad = 0.05;
			if (min - pad == min || max + pad == max)
			{
				pad = Math.Abs(min) * 0.05;
			}

			lower = min - pad;
			upper = max + pad;
		}
		else
		{
			var span = max - min;
			var lowerPad = double.IsFinite(span) ? expand.minMult * span : expand.minMult * max - expand.minMult * min;
			var upperPad = double.IsFinite(span) ? expand.maxMult * span : expand.maxMult * max - expand.maxMult * min;
			lower = min - (lowerPad + expand.minAdd);
			upper = max + upperPad + expand.maxAdd;
		}

		lower = Math.Clamp(lower, -double.MaxValue, double.MaxValue);
		upper = Math.Clamp(upper, -double.MaxValue, double.MaxValue);
		Validate(lower, upper);
		if (lower >= upper)
		{
			throw new GGNetUserException("Scale expansion must leave a nonempty position range.");
		}

		return (lower, upper);
	}

	public static double Normalize(double value, double min, double max)
	{
		var span = max - min;
		return double.IsFinite(span) && double.IsFinite(value - min)
			? (value - min) / span
			: (value / 2 - min / 2) / (max / 2 - min / 2);
	}

	public static double Interpolate(double min, double max, double fraction)
	{
		if (fraction == 0)
		{
			return min;
		}

		if (fraction == 1)
		{
			return max;
		}

		return double.IsFinite(max - min)
			? min + fraction * (max - min)
			: (1 - fraction) * min + fraction * max;
	}

	public static bool NeedsSimpleBreaks(double min, double max)
	{
		var span = max - min;
		return !double.IsFinite(span) || span < minimumTickSearchSpan || span > maximumTickSearchSpan
			|| Math.Max(Math.Abs(min), Math.Abs(max)) / span > maximumTickSearchOffset;
	}

	public static double[] Breaks(double min, double max)
		=> [.. Enumerable.Range(0, 5).Select(i => Interpolate(min, max, i / 4.0)).Distinct()];
}
