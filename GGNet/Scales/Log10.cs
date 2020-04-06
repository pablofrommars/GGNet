using System;
using System.Linq;

using GGNet.Formats;
using GGNet.Transformations;

namespace GGNet.Scales
{
    public class Log10 : Position<double>
    {
        private readonly IFormatter<double> formatter;

        private static readonly DoubleFormatter defaultFormatter = new DoubleFormatter("0.##e0");

        public Log10((double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            IFormatter<double> formatter = null)
            : base(Transformations.Log10.Instance, expand ?? (0.05, 0, 0.05, 0))
        {
            Limits = limits ?? (null, null);

            this.formatter = formatter ?? defaultFormatter;
        }

        public override Guide Guide => Guide.None;

        public override void Set(bool grid)
        {
            SetRange(Limits.min ?? _min ?? 0.0, Limits.max ?? _max ?? 0.0);

            if (!grid)
            {
                return;
            }

            var breaks = Log10Utils.Breaks(Range.min, Range.max);
            if (breaks.Length > 0)
            {
                Breaks = breaks;
                MinorBreaks = Utils.minor_breaks(breaks, Range.min, Range.max);
            }
            else
            {
                Breaks = Log10Utils.MinorBreaks(Range.min, Range.max);
            }

            var labels = new (double, string)[Breaks.Count()];

            for (var i = 0; i < labels.Length; i++)
            {
                var b = Breaks.ElementAt(i);
                labels[i] = (b, formatter.Format(transformation.Inverse(b)));
            }

            Labels = labels;
        }

        public override double Map(double key) => transformation.Apply(key);

        public override ITransformation<double> RangeTransformation => transformation;
    }

    public static class Log10Utils
    {
        public static double[] MinorBreaks(double min, double max)
        {
            var mult = Math.Pow(10.0, Math.Ceiling(max));

            return new[]
            { 
                Math.Log10(0.01 * mult), Math.Log10(0.03 * mult), Math.Log10(0.05 * mult), 
                Math.Log10(0.1 * mult), Math.Log10(0.3 * mult), Math.Log10(0.5 * mult)
            };
        }

        public static double[] Breaks(double min, double max, int n = 5)
        {
            var bmin = Math.Floor(min);
            var bmax = Math.Ceiling(max);

            if (bmin == bmax)
            {
                return new[] { Math.Pow(10.0, bmin) };
            }

            var by = Math.Floor((bmax - bmin) / n) + 1.0;

            while (by > 1)
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

            return new double[0];
        }
    }
}