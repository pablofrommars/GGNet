using GGNet.Formats;
using GGNet.Scales.Common;

using static System.Math;

namespace GGNet.Scales;

internal class FillContinuous(
	string[] colors,
	int m = 5,
	IFormatter<double>? formatter = null) : Scale<double, string>()
{
	private readonly string[] colors = Palettes.Utils.NonEmpty(colors, "Scale_Fill_Continuous");
	private readonly int m = m;
	private readonly IFormatter<double> formatter = formatter ?? new DoubleFormatter("0.##");

	protected (double min, double max) limits = (0.0, 0.0);

	// Initialization is tracked separately: (0, 0) is a legitimate trained state, so a leading
	// zero observation must not be mistaken for "nothing trained yet".
	private bool trained;

	public override Guide Guide => Guide.ColorBar;

	public override void Train(double key)
	{
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

		var extended = Wilkinson.Extended(limits.min, limits.max, m);
		if (extended is null)
		{
			return;
		}

		var labels = new (string value, string label)[extended.Length];
		var breaks = new string[extended.Length];

		for (var i = 0; i < extended.Length; i++)
		{
			var value = extended[extended.Length - i - 1];
			var mapped = Map(value);

			breaks[i] = mapped;
			labels[i] = (mapped, formatter.Format(value));
		}

		Breaks = breaks;
		Labels = labels;
	}

	public override string Map(double key)
	{
		if (limits.max > limits.min)
		{
			return colors[Max(Min((int)((key - limits.min) / (limits.max - limits.min) * (colors.Length - 1)), colors.Length - 1), 0)];
		}
		else
		{
			// Constant data has no gradient to place a key on: every key takes the middle color,
			// the same index a mid-range key would take above. Returning empty here would make
			// every fill consumer skip the mark and the layer would vanish.
			return colors[(colors.Length - 1) / 2];
		}
	}

	public override void Clear()
	{
		limits = (0.0, 0.0);
		trained = false;
	}
}
