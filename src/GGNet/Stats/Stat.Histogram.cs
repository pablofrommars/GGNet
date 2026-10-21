namespace GGNet;

public static partial class Stat
{
	private static double[] HistogramEdges(double min, double max, int bins)
	{
		if (bins == int.MaxValue)
		{
			throw HistogramIntervalError();
		}

		var edges = new double[bins + 1];

		if (min == max)
		{
			HistogramConstantEdges(edges, min, step: 1);

			if (!HistogramEdgesIncrease(edges))
			{
				var magnitude = Math.Abs(min);
				var spacing = magnitude == double.MaxValue
					? magnitude - Math.BitDecrement(magnitude)
					: Math.BitIncrement(magnitude) - magnitude;

				HistogramConstantEdges(edges, min, Math.Max(1, 2 * spacing));
			}
		}
		else
		{
			edges[0] = min;
			edges[^1] = max;

			var span = max - min;
			var width = span / bins;

			for (var i = 1; i < bins; i++)
			{
				if (double.IsNormal(width))
				{
					edges[i] = min + i * width;
				}
				else if (double.IsFinite(span))
				{
					var exponent = Math.ILogB(Math.Max(Math.Abs(min), Math.Abs(max)));
					var lower = Math.ScaleB(min, -exponent);
					var upper = Math.ScaleB(max, -exponent);

					edges[i] = Math.ScaleB(lower + (upper - lower) * ((double)i / bins), exponent);
				}
				else
				{
					var fraction = (double)i / bins;

					edges[i] = min * (1 - fraction) + max * fraction;
				}
			}
		}

		if (!HistogramEdgesIncrease(edges))
		{
			throw HistogramIntervalError();
		}

		return edges;
	}

	private static void HistogramConstantEdges(double[] edges, double value, double step)
	{
		var bins = edges.Length - 1;

		for (var direction = 0; direction < 2; direction++)
		{
			for (var i = 0; i <= bins; i++)
			{
				edges[i] = direction == 0 ? value + i * step : value - (bins - i) * step;
			}

			if (HistogramEdgesIncrease(edges))
			{
				return;
			}
		}
	}

	private static bool HistogramEdgesIncrease(double[] edges)
	{
		if (!double.IsFinite(edges[0]))
		{
			return false;
		}

		for (var i = 1; i < edges.Length; i++)
		{
			if (!double.IsFinite(edges[i]) || edges[i] <= edges[i - 1])
			{
				return false;
			}
		}

		return true;
	}

	private static Exceptions.GGNetUserException HistogramIntervalError()
		=> new("Stat.Bin cannot represent the requested number of distinct finite intervals over this range. Use fewer bins or rescale the selected values.");
}
