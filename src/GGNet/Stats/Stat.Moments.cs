using System.Numerics;

using GGNet.Exceptions;

namespace GGNet;

public static partial class Stat
{
	private static (double mean, double sd, int exponent) MeanSd(IReadOnlyList<double> values)
	{
		var sum = 0.0;
		var maximum = 0.0;
		var constant = true;

		for (var i = 0; i < values.Count; i++)
		{
			ValidateStatNumber(values[i], "Sample value");
			sum += values[i];
			maximum = Math.Max(maximum, Math.Abs(values[i]));
			constant &= values[i] == values[0];
		}

		if (constant)
		{
			return (values[0], 0.0, 0);
		}

		var mean = sum / values.Count;
		var ss = 0.0;
		var needsScaling = !double.IsFinite(mean);

		for (var i = 0; i < values.Count; i++)
		{
			var d = values[i] - mean;
			var square = d * d;
			needsScaling |= d != 0.0 && !double.IsNormal(square);
			ss += square;
		}

		var variance = ss / (values.Count - 1);

		if (!needsScaling && double.IsNormal(variance))
		{
			return (mean, Math.Sqrt(variance), 0);
		}

		var exponent = Math.ILogB(maximum);
		mean = ExactMean(values);
		var scaledOrigin = Math.ScaleB(values[0], -exponent);
		var scaledOffset = ExactMean(values, exponent, values[0]);
		ss = 0.0;

		for (var i = 0; i < values.Count; i++)
		{
			var d = (Math.ScaleB(values[i], -exponent) - scaledOrigin) - scaledOffset;
			ss += d * d;
		}

		return (mean, Math.Sqrt(ss / (values.Count - 1)), exponent);
	}

	private static double ExactMean(IReadOnlyList<double> values, int exponent = 0, double origin = 0.0)
	{
		var sum = BigInteger.Zero;

		for (var i = 0; i < values.Count; i++)
		{
			sum += BinaryInteger(values[i]);
		}

		sum -= BinaryInteger(origin) * values.Count;

		var sign = sum.Sign;

		if (sign == 0)
		{
			return 0.0;
		}

		sum = BigInteger.Abs(sum);
		var denominator = new BigInteger(values.Count);
		var bitsAboveDenominator = (int)(sum.GetBitLength() - denominator.GetBitLength());

		if (bitsAboveDenominator >= 0 ? sum < (denominator << bitsAboveDenominator) : (sum << -bitsAboveDenominator) < denominator)
		{
			bitsAboveDenominator--;
		}

		var shift = Math.Max(exponent, bitsAboveDenominator - 52);

		if (shift >= 0)
		{
			denominator <<= shift;
		}
		else
		{
			sum <<= -shift;
		}
		var quotient = BigInteger.DivRem(sum, denominator, out var remainder);
		var comparison = (remainder * 2).CompareTo(denominator);

		if (comparison > 0 || (comparison == 0 && !quotient.IsEven))
		{
			quotient++;
		}

		return Math.ScaleB(sign * (double)quotient, shift - 1074 - exponent);
	}

	private static BigInteger BinaryInteger(double value)
	{
		var bits = BitConverter.DoubleToUInt64Bits(value);
		var exponent = (int)((bits >> 52) & 0x7ff);
		var significand = bits & 0xfffffffffffff;

		if (exponent != 0)
		{
			significand |= 1UL << 52;
		}

		var term = new BigInteger(significand) << Math.Max(0, exponent - 1);
		return (bits >> 63) == 0 ? term : -term;
	}

	private static (double center, double lower, double upper) SummaryInterval(IReadOnlyList<double> values, double spread)
	{
		var (center, sd, exponent) = MeanSd(values);

		if (sd == 0.0 || spread == 0.0)
		{
			return (center, center, center);
		}

		var spreadExponent = Math.ILogB(Math.Abs(spread));
		var scaledSpread = Math.ScaleB(spread, -spreadExponent);
		var intervalExponent = exponent + spreadExponent;
		var centerExponent = center == 0.0 ? intervalExponent : Math.ILogB(Math.Abs(center));
		var outputExponent = Math.Max(intervalExponent, centerExponent);
		var scaledCenter = exponent == 0 ? Math.ScaleB(center, -outputExponent) : ExactMean(values, outputExponent);
		var scaledSd = Math.ScaleB(sd, intervalExponent - outputExponent);
		var lower = Math.ScaleB(Math.FusedMultiplyAdd(-scaledSpread, scaledSd, scaledCenter), outputExponent);
		var upper = Math.ScaleB(Math.FusedMultiplyAdd(scaledSpread, scaledSd, scaledCenter), outputExponent);

		if (!double.IsFinite(lower) || !double.IsFinite(upper))
		{
			throw new GGNetUserException("Summary interval endpoints cannot be represented as finite doubles. Reduce spread or rescale the selected values.");
		}

		return (center, lower, upper);
	}

	private static double ValidateStatNumber(double value, string name)
	{
		if (!double.IsFinite(value))
		{
			throw new GGNetUserException(FormattableString.Invariant($"{name} must be finite; received {value}. Remove or replace nonfinite values before calculating statistics."));
		}

		return value;
	}
}
