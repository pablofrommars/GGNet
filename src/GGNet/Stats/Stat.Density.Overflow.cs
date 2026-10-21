namespace GGNet;

public static partial class Stat
{
	private static double OverflowDensityAt(List<double> values, double h, double at)
	{
		var bandwidthUnits = DensityUnits(h);
		var maximum = decimal.MinValue;

		for (var i = 0; i < values.Count; i++)
		{
			if (DensityDecimalExponent(at, values[i], h, bandwidthUnits) is decimal exponent)
			{
				maximum = Math.Max(maximum, exponent);
			}
		}

		var sum = 0m;
		var compensation = 0m;

		for (var i = 0; i < values.Count; i++)
		{
			if (DensityDecimalExponent(at, values[i], h, bandwidthUnits) is not decimal exponent)
			{
				continue;
			}

			var term = DensityDecimalExp(exponent - maximum);
			var next = sum + term;
			compensation += sum >= term ? (sum - next) + term : (term - next) + sum;
			sum = next;
		}

		const decimal sqrtTwoPi = 2.5066282746310005024157652848m;
		const decimal ln2 = 0.6931471805599453094172321215m;
		var kernelPower = (int)decimal.Floor(maximum / ln2);
		var numerator = (sum + compensation) * DensityDecimalExp(maximum - kernelPower * ln2);

		for (var i = 0; i < kernelPower + 50; i++)
		{
			numerator *= 2m;
		}

		for (var i = 0; i > kernelPower + 50; i--)
		{
			numerator /= 2m;
		}

		var relative = numerator / (values.Count * bandwidthUnits * sqrtTwoPi);

		if (relative >= 1m)
		{
			return double.PositiveInfinity;
		}

		var power = 971;

		while (relative < 0.5m && relative > 0m)
		{
			relative *= 2m;
			power--;
		}

		var significand = decimal.Round(relative * 9007199254740992m, 0, MidpointRounding.ToEven);

		return Math.ScaleB((double)significand, power);
	}

	private static decimal? DensityDecimalExponent(double at, double value, double h, decimal bandwidthUnits)
	{
		var difference = at - value;

		if (Math.Abs(difference / h) >= 16.0)
		{
			return null;
		}

		var differenceUnits = Math.Max(Math.Abs(at), Math.Abs(value)) <= 32.0 * h
			? DensityUnits(at) - DensityUnits(value)
			: DensityUnits(difference);
		var u = differenceUnits / bandwidthUnits;

		return -u * u / 2m;
	}

	private static decimal DensityUnits(double value)
	{
		var bits = BitConverter.DoubleToUInt64Bits(Math.Abs(value));
		var exponent = (int)(bits >> 52);
		var significand = bits & 0x000fffffffffffffUL;

		if (exponent != 0)
		{
			significand |= 0x0010000000000000UL;
		}

		var units = (decimal)significand * (1UL << Math.Max(0, exponent - 1));

		return value < 0.0 ? -units : units;
	}

	private static decimal DensityDecimalExp(decimal exponent)
	{
		var reduced = exponent / 256m;
		var sum = 1m;
		var term = 1m;

		for (var i = 1; i <= 40; i++)
		{
			term = term * reduced / i;
			sum += term;

			if (term == 0m)
			{
				break;
			}
		}

		for (var i = 0; i < 8; i++)
		{
			sum *= sum;
		}

		return sum;
	}
}
