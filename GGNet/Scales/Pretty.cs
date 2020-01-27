using System.Collections.Generic;

using NodaTime;

using static System.Math;

namespace GGNet.Scales
{
    public static class Pretty
    {
        private static readonly double h = 1.5;
        private static readonly double h5 = 0.5 + 1.5 * h;
        private static readonly double shrink_sml = 0.75;
        private static readonly int eps_correction = 0;
        private static readonly double rounding_eps = 1e-10;

        private static (double lower, double upper, int n) _pretty(double lo, double up, int ndiv, int min_n, double shrink_sml, double h, double h5, int eps_correction)
        {
            double dx, cell, unit, _base, U;
            double ns, nu;
            int k;
            bool i_small;

            dx = up - lo;
            /* cell := "scale"	here */
            if (dx == 0 && up == 0)
            { /*  up == lo == 0	 */
                cell = 1;
                i_small = true;
            }
            else
            {
                cell = Max(Abs(lo), Abs(up));
                /* U = upper bound on cell/unit */
                U = 1 + ((h5 >= 1.5 * h + .5) ? 1 / (1 + h) : 1.5 / (1 + h5));
                U *= Max(1, ndiv) * double.Epsilon; // avoid overflow for large ndiv
                                                    /* added times 3, as several calculations here */
                i_small = dx < cell * U * 3;
            }

            /*OLD: cell = FLT_EPSILON+ dx / *ndiv; FLT_EPSILON = 1.192e-07 */
            if (i_small)
            {
                if (cell > 10)
                    cell = 9 + cell / 10;
                cell *= shrink_sml;
                if (min_n > 1) cell /= min_n;
            }
            else
            {
                cell = dx;
                if (ndiv > 1) cell /= ndiv;
            }

            if (cell < 20 * double.MinValue)
            {
                cell = 20 * double.MinValue;
            }
            else if (cell * 10 > double.MaxValue)
            {
                cell = .1 * double.MaxValue;
            }
            /* NB: the power can be negative and this relies on exact
               calculation, which glibc's exp10 does not achieve */
            _base = Pow(10.0, Floor(Log10(cell))); /* base <= cell < 10*base */

            /* unit : from { 1,2,5,10 } * base
             *	 such that |u - cell| is small,
             * favoring larger (if h > 1, else smaller)  u  values;
             * favor '5' more than '2'  if h5 > h  (default h5 = .5 + 1.5 h) */
            unit = _base;
            if ((U = 2 * _base) - cell < h * (cell - unit))
            {
                unit = U;
                if ((U = 5 * _base) - cell < h5 * (cell - unit))
                {
                    unit = U;
                    if ((U = 10 * _base) - cell < h * (cell - unit)) unit = U;
                }
            }
            /* Result: c := cell,  u := unit,  b := base
             *	c in [	1,	      (2+ h) /(1+h) ] b ==> u=  b
             *	c in ( (2+ h)/(1+h),  (5+2h5)/(1+h5)] b ==> u= 2b
             *	c in ( (5+2h)/(1+h), (10+5h) /(1+h) ] b ==> u= 5b
             *	c in ((10+5h)/(1+h),	         10 ) b ==> u=10b
             *
             *	===>	2/5 *(2+h)/(1+h)  <=  c/u  <=  (2+h)/(1+h)	*/

            ns = Floor(lo / unit + rounding_eps);
            nu = Ceiling(up / unit - rounding_eps);

            if (eps_correction > 0 && (eps_correction > 1 || !i_small))
            {
                if (lo != 0) lo *= (1 - double.Epsilon); else lo = -double.MinValue;
                if (up != 0) up *= (1 + double.Epsilon); else up = +double.MaxValue;
            }

            while (ns * unit > lo + rounding_eps * unit) ns--;
            while (nu * unit < up - rounding_eps * unit) nu++;

            k = (int)(0.5 + nu - ns);
            if (k < min_n)
            {
                k = min_n - k;
                if (ns >= 0)
                {
                    nu += k / 2;
                    ns -= k / 2 + k % 2;/* ==> nu-ns = old(nu-ns) + min_n -k = min_n */
                }
                else
                {
                    ns -= k / 2;
                    nu += k / 2 + k % 2;
                }
                ndiv = min_n;
            }
            else
            {
                ndiv = k;
            }

            if (ns * unit < lo) lo = ns * unit;
            if (nu * unit > up) up = nu * unit;

            return (lo, up, ndiv);
        }

        public static double delta(double min, double max, int n = 5)
        {
            var z = _pretty(min, max, n, (int)(5 / 3.0), shrink_sml, h, h5, eps_correction);

            if (z.n == 0)
            {
                return 0;
            }

            return (z.upper - z.lower) / z.n;
        }

        public static double[] pretty(double min, double max, int n = 5)
        {
            var z = _pretty(min, max, n, (int)(5 / 3.0), shrink_sml, h, h5, eps_correction);

            if (z.n == 0)
            {
                return null;
            }

            var res = new double[z.n + 1];

            var delta = (z.upper - z.lower) / z.n;

            for (var i = 0; i <= z.n; i++)
            {
                res[i] = z.lower + i * delta;
                if (eps_correction == 0 && Abs(res[i]) < 1e-14 * delta)
                {
                    res[i] = 0;
                }
            }

            return res;
        }

        public static ulong _diff(LocalDateTime min, LocalDateTime max)
        {
            var period = max - min;

            return (ulong)(period.Years * 3600 * 24 * 365
                + period.Months * 3600 * 24 * 30
                + period.Days * 3600 * 24
                + period.Hours * 3600
                + period.Minutes * 60
                + period.Seconds);
        }

        public static LocalDateTime[] pretty_year(LocalDateTime min, LocalDateTime max, ulong diff, int n)
        {
            var length = (int)pretty(1.0, diff / (double)(3600 * 24 * 365), n)[1];

            var start = new LocalDateTime(min.Year, 1, 1, 0, 0, 0);
            var end = new LocalDateTime(max.Year + 1, 1, 1, 0, 0, 0);

            var results = new LocalDateTime[(int)((end.Year - start.Year) / (double)length + 1)];

            var i = 0;
            var current = start;
            while (current <= end)
            {
                results[i++] = current;
                current = current.PlusYears(length);
            }

            return results;
        }

        private static readonly int[] length_months = new[] { 1, 2, 3, 4, 6, 12 };

        public static LocalDateTime[] pretty_month(LocalDateTime min, LocalDateTime max, ulong diff, int n)
        {
            var span = diff / (double)(3600 * 24 * 30);

            var length = 12;
            {
                var minfit = double.MaxValue;

                foreach (var l in length_months)
                {
                    var fit = Abs(span - l * n);
                    if (fit < minfit)
                    {
                        length = l;
                        minfit = fit;
                    }
                }
            }

            var start = min;
            {
                var lower = new LocalDateTime(min.Year, 1, 1, 0, 0, 0);

                var minfit = double.MaxValue;

                var current = lower;

                while (current <= min)
                {
                    var fit = _diff(min, current);
                    if (fit < minfit)
                    {
                        start = current;
                        minfit = fit;
                    }
                    current = current.PlusMonths(length);
                }
            }

            var end = max;
            {
                var lower = new LocalDateTime(max.Year, 1, 1, 0, 0, 0);
                var upper = new LocalDateTime(max.Year + 1, 1, 1, 0, 0, 0);

                var minfit = double.MaxValue;

                var current = lower;

                while (current <= upper)
                {
                    if (current >= max)
                    {
                        var fit = _diff(max, current);
                        if (fit < minfit)
                        {
                            end = current;
                            minfit = fit;
                        }
                    }
                    current = current.PlusMonths(length);
                }
            }

            var period = end - start;
            var results = new LocalDateTime[(int)((period.Years * 12 + period.Months) / (double)length + 1)];

            {
                var i = 0;
                var current = start;
                while (current <= end)
                {
                    results[i++] = current;
                    current = current.PlusMonths(length);
                }
            }

            return results;
        }

        public static LocalDateTime[] pretty_day(LocalDateTime min, LocalDateTime max, ulong diff, int n)
        {
            var length = (int)pretty(1.0, diff / (double)(3600 * 24), n)[1];

            var start = min.Date.At(new LocalTime());
            var end = max.Date.PlusDays(1).At(new LocalTime());

            var results = new List<LocalDateTime>();

            var current = start;
            while (current <= end)
            {
                results.Add(current);
                current = current.PlusDays(length);
            }

            return results.ToArray();
        }

        private static readonly int[] length_hours = new[] { 1, 2, 3, 4, 6, 8, 12, 24 };

        public static LocalDateTime[] pretty_hour(LocalDateTime min, LocalDateTime max, ulong diff, int n)
        {
            var span = diff / 3600.0;

            var length = 24;
            {
                var minfit = double.MaxValue;

                foreach (var l in length_hours)
                {
                    var fit = Abs(span - l * n);
                    if (fit < minfit)
                    {
                        length = l;
                        minfit = fit;
                    }
                }
            }

            var start = min;
            {
                var lower = min.Date.At(new LocalTime());

                var minfit = double.MaxValue;

                var current = lower;

                while (current <= min)
                {
                    var fit = _diff(min, current);
                    if (fit < minfit)
                    {
                        start = current;
                        minfit = fit;
                    }
                    current = current.PlusHours(length);
                }
            }

            var end = max;
            {
                var lower = max.Date.At(new LocalTime());
                var upper = max.Date.PlusDays(1).At(new LocalTime());

                var minfit = double.MaxValue;

                var current = lower;

                while (current <= upper)
                {
                    if (current >= max)
                    {
                        var fit = _diff(max, current);
                        if (fit < minfit)
                        {
                            end = current;
                            minfit = fit;
                        }
                    }
                    current = current.PlusHours(length);
                }
            }

            var period = end - start;
            var results = new LocalDateTime[(int)((period.Days * 24 + period.Hours) / (double)length + 1)];

            {
                var i = 0;
                var current = start;
                while (current <= end)
                {
                    results[i++] = current;
                    current = current.PlusHours(length);
                }
            }

            return results;
        }

        private static readonly int[] length_minutes = new[] { 1, 2, 5, 10, 15, 30, 60 };

        public static LocalDateTime[] pretty_minute(LocalDateTime min, LocalDateTime max, ulong diff, int n)
        {
            var span = diff / 60.0;

            var length = 60;
            {
                var minfit = double.MaxValue;

                foreach (var l in length_minutes)
                {
                    var fit = Abs(span - l * n);
                    if (fit < minfit)
                    {
                        length = l;
                        minfit = fit;
                    }
                }
            }

            var start = min;
            {
                var lower = min.With(TimeAdjusters.TruncateToHour);

                var minfit = double.MaxValue;

                var current = lower;

                while (current <= min)
                {
                    var fit = _diff(min, current);
                    if (fit < minfit)
                    {
                        start = current;
                        minfit = fit;
                    }
                    current = current.PlusMinutes(length);
                }
            }

            var end = max;
            {
                var lower = max.With(TimeAdjusters.TruncateToHour);
                var upper = max.PlusHours(1).With(TimeAdjusters.TruncateToHour);

                var minfit = double.MaxValue;

                var current = lower;

                while (current <= upper)
                {
                    if (current >= max)
                    {
                        var fit = _diff(max, current);
                        if (fit < minfit)
                        {
                            end = current;
                            minfit = fit;
                        }
                    }
                    current = current.PlusMinutes(length);
                }
            }

            var period = end - start;
            var results = new LocalDateTime[(int)((period.Hours * 60 + period.Minutes) / (double)length + 1)];

            {
                var i = 0;
                var current = start;
                while (current <= end)
                {
                    results[i++] = current;
                    current = current.PlusMinutes(length);
                }
            }

            return results;
        }

        private static readonly int[] length_secondes = new[] { 1, 2, 5, 10, 15, 30, 60 };

        public static LocalDateTime[] pretty_second(LocalDateTime min, LocalDateTime max, ulong diff, int n)
        {
            var length = 60;
            {
                var minfit = double.MaxValue;

                foreach (var l in length_secondes)
                {
                    var fit = Abs((double)diff - l * n);
                    if (fit < minfit)
                    {
                        length = l;
                        minfit = fit;
                    }
                }
            }

            var start = min;
            {
                var lower = min.With(TimeAdjusters.TruncateToMinute);

                var minfit = double.MaxValue;

                var current = lower;

                while (current <= min)
                {
                    var fit = _diff(min, current);
                    if (fit < minfit)
                    {
                        start = current;
                        minfit = fit;
                    }
                    current = current.PlusSeconds(length);
                }
            }

            var end = max;
            {
                var lower = max.With(TimeAdjusters.TruncateToMinute);
                var upper = max.PlusMinutes(1).With(TimeAdjusters.TruncateToMinute);

                var minfit = double.MaxValue;

                var current = lower;

                while (current <= upper)
                {
                    if (current >= max)
                    {
                        var fit = _diff(max, current);
                        if (fit < minfit)
                        {
                            end = current;
                            minfit = fit;
                        }
                    }
                    current = current.PlusSeconds(length);
                }
            }

            var period = end - start;
            var results = new LocalDateTime[(int)((period.Minutes * 60 + period.Seconds) / (double)length + 1)];

            {
                var i = 0;
                var current = start;
                while (current <= end)
                {
                    results[i++] = current;
                    current = current.PlusSeconds(length);
                }
            }

            return results;
        }

        public static LocalDateTime[] pretty(LocalDateTime min, LocalDateTime max, int n = 5)
        {
            var diff = _diff(min, max);

            return (diff / (double)n) switch
            {
                double sec when sec > 3600 * 24 * 365 => pretty_year(min, max, diff, n),
                double sec when sec > 3600 * 24 * 30 => pretty_month(min, max, diff, n),
                double sec when sec > 3600 * 24 => pretty_day(min, max, diff, n),
                double sec when sec > 3600 => pretty_hour(min, max, diff, n),
                double sec when sec > 60 => pretty_minute(min, max, diff, n),
                _ => pretty_second(min, max, diff, n),
            };
        }
    }
}
