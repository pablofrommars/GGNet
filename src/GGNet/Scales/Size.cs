using GGNet.Formats;
using GGNet.Scales.Common;
using GGNet.Transformations;

using static System.Math;

namespace GGNet.Scales;

internal class SizeContinuous : Continuous<double>
{
	protected readonly bool defined;
	protected (double min, double max) domain;
	private bool trained;
	protected readonly (double min, double max) outputRange;
	protected readonly bool squish;
	private readonly IFormatter<double> labelFormatter;

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
			domain = limits.Value;
		}
		else
		{
			domain = (0.0, 0.0);
		}

		outputRange = range ?? (0.0, 1.0);
		squish = oob;

		labelFormatter = formatter ?? new DoubleFormatter("0.##");
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
			domain = (key, key);
			trained = true;
		}
		else
		{
			domain = (Min(domain.min, key), Max(domain.max, key));
		}
	}

	public override void Commit(bool grid)
	{
		if (!grid)
		{
			return;
		}

		var breaks = Wilkinson.Extended(domain.min, domain.max);
		if (breaks is null)
		{
			return;
		}

		var labels = new (double value, string label)[breaks.Length];

		for (var i = 0; i < breaks.Length; i++)
		{
			var value = breaks[i];

			labels[i] = (Map(value), labelFormatter.Format(value));
		}

		Breaks = breaks;
		Labels = labels;
	}

	public override double Map(double key)
	{
		if (!squish)
		{
			if (key < domain.min || key > domain.max)
			{
				return double.NaN;
			}
		}

		if (key < domain.min)
		{
			return outputRange.min;
		}
		else if (key > domain.max)
		{
			return outputRange.max;
		}

		if (domain.max <= domain.min)
		{
			return outputRange.min + (outputRange.max - outputRange.min) / 2.0;
		}

		if (domain.min >= 0)
		{
			return outputRange.min + (Sqrt(key) - Sqrt(domain.min)) / (Sqrt(domain.max) - Sqrt(domain.min)) * (outputRange.max - outputRange.min);
		}
		else
		{
			return outputRange.min + Sqrt(key - domain.min) / Sqrt(domain.max - domain.min) * (outputRange.max - outputRange.min);
		}
	}

	public override void Clear()
	{
		if (defined)
		{
			return;
		}

		domain = (0.0, 0.0);
		trained = false;
	}
}
