using GGNet.Formats;
using GGNet.Transformations;
using GGNet.Scales.Common;
using GGNet.Exceptions;

namespace GGNet.Scales;

internal sealed class Log10 : Position<double>
{
	private readonly IFormatter<double> labelFormatter;

	private static readonly DoubleFormatter defaultFormatter = new("0.##e0");

	public Log10((double? min, double? max)? limits = null,
		(double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
		IFormatter<double>? formatter = null)
		: base(Transformations.Log10.Instance, expand ?? (0.05, 0, 0.05, 0))
	{
		Limits = limits ?? (null, null);

		labelFormatter = formatter ?? defaultFormatter;
	}

	public override Guide Guide => Guide.None;

	public override void Commit(bool grid)
	{
		if (!CommitViewRange())
		{
			SetRange(Endpoint(Limits.min, _min), Endpoint(Limits.max, _max));
		}

		if (!grid)
		{
			return;
		}

		var min = Math.Max(Range.min, Math.Log10(double.Epsilon));
		var max = Math.Min(Range.max, Math.Log10(double.MaxValue));
		var breaks = min <= max ? Log10Utils.Breaks(min, max) : [];
		MinorBreaks = [];
		if (breaks.Length > 0)
		{
			Breaks = breaks;
			MinorBreaks = Utils.MinorBreaks(breaks, min, max) ?? [];
		}
		else
		{
			Breaks = min <= max ? Log10Utils.MinorBreaks(max) : [];
		}

		Breaks = [.. Breaks.Where(b => double.IsFinite(b) && double.IsFinite(transformation.Inverse(b)) && transformation.Inverse(b) > 0)];
		MinorBreaks = [.. MinorBreaks.Where(double.IsFinite)];
		Labels = [.. Breaks.Select(b => (b, labelFormatter.Format(transformation!.Inverse(b))))];
	}

	public override double Map(double key)
	{
		if (key <= 0 || double.IsInfinity(key))
		{
			throw new GGNetUserException("Log10 positions require positive finite values; filter nonpositive data or use a continuous scale.");
		}

		return transformation!.Apply(key);
	}

	public override double? Unmap(double value) => transformation!.Inverse(value);

	public override string? Label(double value) => labelFormatter.Format(value);

	public override ITransformation<double> RangeTransformation => transformation!;
}

internal static class Log10Utils
{
	private static readonly double[] minorMultipliers = [0.01, 0.03, 0.05, 0.1, 0.3, 0.5];

	public static double[] MinorBreaks(double max)
	{
		var mult = Math.Pow(10.0, Math.Ceiling(max));
		if (!double.IsFinite(mult) || mult == 0)
		{
			return [.. minorMultipliers.Select(v => Math.Log10(v) + Math.Ceiling(max))];
		}

		return [
			Math.Log10(0.01 * mult), Math.Log10(0.03 * mult), Math.Log10(0.05 * mult),
			Math.Log10(0.1 * mult), Math.Log10(0.3 * mult), Math.Log10(0.5 * mult)
		];
	}

	public static double[] Breaks(double min, double max, int n = 5)
	{
		PositionRange.Validate(min, max);
		var bmin = Math.Floor(min);
		var bmax = Math.Ceiling(max);

		if (bmin == bmax)
		{
			return [bmin];
		}

		var by = Math.Floor((bmax - bmin) / n) + 1.0;

		for (var attempt = 0; by > 1 && attempt < n; attempt++)
		{
			var N = (int)((bmax - bmin) / by) + 1;
			var breaks = new double[N];
			var relevant = 0;

			var b = bmin;
			for (var i = 0; i < N; i++)
			{
				breaks[i] = b;

				if (min <= b && b <= max)
				{
					relevant++;
				}

				b += by;
			}

			if (relevant >= (n - 3))
			{
				return breaks;
			}

			by -= 1.0;
		}

		return [];
	}
}
