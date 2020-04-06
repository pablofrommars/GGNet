using System;

using GGNet.Transformations;
using GGNet.Formats;

using static System.Math;

/*
 *  Talbot, Lin, and Hanrahan. An Extension of Wilkinson’s Algorithm for Positioning Tick Labels on Axes, Infovis 2010.
 */

namespace GGNet.Scales
{
    public class Extended : Position<double>
    {
        private readonly IFormatter<double> formatter;

        public Extended(ITransformation<double> transformation = null,
            (double? min, double? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            IFormatter<double> formatter = null)
            : base(transformation, expand ?? (0.05, 0, 0.05, 0))
        {
            Limits = limits ?? (null, null);

            this.formatter = formatter ?? Standard<double>.Instance;
        }

        public override Guide Guide => Guide.None;

        public override void Set(bool grid)
        {
            SetRange(Limits.min ?? _min ?? 0.0, Limits.max ?? _max ?? 0.0);

            if (!grid)
            {
                return;
            }

            var breaks = Wilkinson.extended(Range.min, Range.max);
            if (breaks == null)
            {
                breaks = Pretty.pretty(Range.min, Range.max);
            }

            var minorBreaks = Utils.minor_breaks(breaks, Range.min, Range.max);

            var labels = new (double, string)[breaks.Length];

            for (var i = 0; i < labels.Length; i++)
            {
                labels[i] = (breaks[i], formatter.Format(transformation.Inverse(breaks[i])));
            }

            for (var i = 0; i < minorBreaks.Length; i++)
            {
                minorBreaks[i] = minorBreaks[i];
            }

            Breaks = breaks;
            MinorBreaks = minorBreaks;
            Labels = labels;
        }

        public override double Map(double key) => transformation.Apply(key);

        public override ITransformation<double> RangeTransformation => transformation;
    }

    public static class Wilkinson
    {
        private static readonly double[] Q = new[] { 1.0, 5.0, 2.0, 2.5, 4.0, 3.0 };
        private static readonly double[] w = new[] { 0.25, 0.2, 0.5, 0.05 };
        private static double floored_mod(double a, double n) => a - n * Floor(a / n);

        private static double pow10(int i)
        {
            var a = 1.0;
            for (int j = 0; j < i; j++)
            {
                a *= 10;
            }

            return a;
        }

        private static readonly double eps = double.Epsilon * 100;

        private static double simplicity(double q, int j, double lmin, double lmax, double lstep)
        {
            var v = (floored_mod(lmin, lstep) < eps && lmin <= 0 && lmax >= 0) ? 1 : 0;
            return 1.0 - Array.IndexOf(Q, q) / (Q.Length - 1.0) - j + v;
        }

        private static double max_simplicity(double q, int j) =>
            1.0 - Array.IndexOf(Q, q) / (Q.Length - 1.0) - j + 1.0;

        private static double coverage(double dmin, double dmax, double lmin, double lmax) =>
            1.0 - 0.5 * (((dmax - lmax) * (dmax - lmax) + (dmin - lmin) * (dmin - lmin)) / ((0.1 * (dmax - dmin)) * (0.1 * (dmax - dmin))));

        private static double max_coverage(double dmin, double dmax, double span)
        {
            var range = dmax - dmin;

            if (span > range)
            {
                return 1.0 - 0.25 * (span - range) * (span - range) / (0.01 * range * range);
            }
            else
            {
                return 1.0;
            }
        }

        private static double density(int k, int m, double dmin, double dmax, double lmin, double lmax)
        {
            var r = -(k - 1.0) / (lmax - lmin);
            var rt = -(m - 1.0) / (Max(lmax, dmax) - Min(dmin, lmin));

            return 2.0 - Max(r / rt, rt / r);
        }

        private static double max_density(int k, int m) => k >= m ? 2.0 - (k - 1.0) / (m - 1.0) : 1.0;

        public static double[] extended(double dmin, double dmax, int m = 5, bool onlyLoose = false)
        {
            var bscore = -2.0;
            var from = 0.0;
            var to = 0.0;
            var by = 0.0;

            if (dmax - dmin < eps)
            {
                //return(seq(from=dmin, to=dmax, length.out=m))
                return null;
            }

            var j = 1;
            while (j < int.MaxValue)
            {
                foreach (var q in Q)
                {
                    var sm = max_simplicity(q, j);
                    if (w[0] * sm + w[1] + w[2] + w[3] < bscore)
                    {
                        j = int.MaxValue - 1;
                        break;
                    }

                    var k = 2;
                    while (k < int.MaxValue)
                    {
                        var dm = max_density(k, m);

                        if (w[0] * sm + w[1] + w[2] * dm + w[3] < bscore)
                            break;

                        var delta = (dmax - dmin) / (k + 1.0) / (j * q);
                        var z = (int)Ceiling(Log10(delta));

                        while (z < int.MaxValue)
                        {
                            var step = j * q * pow10(z);
                            var cm = max_coverage(dmin, dmax, step * (k - 1.0));

                            if (w[0] * sm + w[1] * cm + w[2] * dm + w[3] < bscore)
                                break;

                            for (int start = (int)(Floor(dmax / step - (k - 1)) * j); start <= (int)(Ceiling(dmin / step)) * j; start++)
                            {
                                var lmin = start * step / j;
                                var lmax = lmin + step * (k - 1);

                                var s = simplicity(q, j, lmin, lmax, step);
                                var d = density(k, m, dmin, dmax, lmin, lmax);
                                var c = coverage(dmin, dmax, lmin, lmax);

                                var score = w[0] * s + w[1] * c + w[2] * d + w[3];

                                if (score > bscore && (!onlyLoose || (lmin <= dmin && lmax >= dmax)))
                                {
                                    from = lmin;
                                    to = lmax;
                                    by = step;
                                    bscore = score;
                                }
                            }

                            z++;
                        }

                        k++;
                    }
                }

                j++;
            }

            if (by == 0.0)
            {
                return null;
            }

            var n = (int)((to - from) / by) + 1;
            var results = new double[n];

            for (var i = 0; i < n; i++)
            {
                results[i] = from + i * by;
            }

            return results;
        }
    }
}