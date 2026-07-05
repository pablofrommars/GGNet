using GGNet.Formats;
using GGNet.Scales.Common;
using GGNet.Transformations;

namespace GGNet.Scales;

public sealed class Extended : Position<double>
{
	private readonly IFormatter<double> formatter;
	private readonly bool hide;
	private readonly bool includeMinorBreaks;

	public Extended(ITransformation<double>? transformation = null,
		(double? min, double? max)? limits = null,
		(double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
		IFormatter<double>? formatter = null,
		bool hide = false,
		bool includeMinorBreaks = true)
		: base(transformation, expand ?? (0.05, 0, 0.05, 0))
	{
		Limits = limits ?? (null, null);

		this.formatter = formatter ?? Standard<double>.Instance;
		this.hide = hide;
		this.includeMinorBreaks = includeMinorBreaks;
	}

	public override Guide Guide => Guide.None;

	public override void Commit(bool grid)
	{
		SetRange(Limits.min ?? _min ?? 0.0, Limits.max ?? _max ?? 0.0);

		if (!grid)
		{
			return;
		}

		if (hide)
		{
			return;
		}

		var breaks = Wilkinson.Extended(Range.min, Range.max) ?? Pretty.Run(Range.min, Range.max);
		if (breaks is null)
		{
			return;
		}

		Breaks = breaks;

		if (includeMinorBreaks)
		{
			var minorBreaks = Utils.MinorBreaks(breaks, Range.min, Range.max);

			if (minorBreaks is null)
			{
				return;
			}

			MinorBreaks = minorBreaks;
		}

		var labels = new (double, string)[breaks.Length];

		for (var i = 0; i < labels.Length; i++)
		{
			labels[i] = (breaks[i], formatter.Format(transformation.Inverse(breaks[i])));
		}

		Labels = labels;
	}

	public override double Map(double key) => transformation.Apply(key);

	public override ITransformation<double> RangeTransformation => transformation;
}
