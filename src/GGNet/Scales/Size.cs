using GGNet.Formats;
using GGNet.Scales.Common;
using GGNet.Transformations;

using static System.Math;

namespace GGNet.Scales;

internal class SizeContinuous : Continuous<double>
{
	protected readonly bool defined;
	protected (double min, double max) limits;
	// Initialization is tracked separately: (0, 0) is a legitimate trained state, so a leading
	// zero observation must not be mistaken for "nothing trained yet".
	private bool trained;
	protected readonly (double min, double max) range;
	protected readonly bool oob;
	private readonly IFormatter<double> formatter;

	public SizeContinuous(
		(double min, double max)? limits = null,
		(double min, double max)? range = null,
		bool oob = false,
		IFormatter<double>? formatter = null,
		ITransformation<double>? transformation = null)
		: base(transformation)
	{
		if (limits.HasValue)
		{
			defined = true;
			this.limits = limits.Value;
		}
		else
		{
			this.limits = (0.0, 0.0);
		}

		this.range = range ?? (0.0, 1.0);
		this.oob = oob;

		this.formatter = formatter ?? new DoubleFormatter("0.##");
	}

	public override Guide Guide => Guide.Items;

	public override void Train(double key)
	{
		if (defined)
		{
			return;
		}

		if (!trained)
		{
			limits = (key, key);
			trained = true;
		}
		else
		{
			limits = (Min(limits.min, key), Max(limits.max, key));
		}
	}

	public override void Commit(bool grid)
	{
		if (!grid)
		{
			return;
		}

		var breaks = Wilkinson.Extended(limits.min, limits.max);
		if (breaks is null)
		{
			return;
		}

		var labels = new (double value, string label)[breaks.Length];

		for (var i = 0; i < breaks.Length; i++)
		{
			var value = breaks[i];

			labels[i] = (Map(value), formatter.Format(value));
		}

		Breaks = breaks;
		Labels = labels;
	}

	public override double Map(double key)
	{
		if (!oob)
		{
			if (key < limits.min || key > limits.max)
			{
				return double.NaN;
			}
		}

		if (key < limits.min)
		{
			return range.min;
		}
		else if (key > limits.max)
		{
			return range.max;
		}

		// Constant data has no span to divide by: every key takes the midpoint of the size range.
		// Without this the ratio below is 0/0 and a NaN radius reaches the SVG.
		if (limits.max <= limits.min)
		{
			return range.min + (range.max - range.min) / 2.0;
		}

		if (limits.min >= 0)
		{
			return range.min + (Sqrt(key) - Sqrt(limits.min)) / (Sqrt(limits.max) - Sqrt(limits.min)) * (range.max - range.min);
		}
		else
		{
			return range.min + Sqrt(key - limits.min) / Sqrt(limits.max - limits.min) * (range.max - range.min);
		}
	}

	public override void Clear()
	{
		if (defined)
		{
			return;
		}

		limits = (0.0, 0.0);
		trained = false;
	}
}
