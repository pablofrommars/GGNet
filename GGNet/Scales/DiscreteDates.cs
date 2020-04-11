using System.Collections.Generic;

using NodaTime;

using GGNet.Transformations;

namespace GGNet.Scales
{
    public class DiscretDates : DiscretePosition<LocalDate>
    {
        private static readonly string[] Abbreviations = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        public DiscretDates(ITransformation<LocalDate> transformation = null,
            (LocalDate? min, LocalDate? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
            : base(transformation, limits, expand)
        {
        }

        protected void DayMonth(int start, int end)
        {
            /*
            var delta = (int)Pretty.delta(start, end);

            if (delta == 0)
            {
                return;
            }
            */

            var delta = (end - start + 1) switch
            {
                var _n when _n <= 7 => 1,
                var _n when _n <= 14 => 2,
                var _n when _n <= 35 => 5,
                var _n when _n < 70 => 10,
                _ => 15
            };

            var breaks = new List<double>();
            var labels = new List<(double x, string day)>();
            var titles = new List<(double x, string month)>();

            var mfirst = -1;
            var mlast = -1;

            var month = 0;

            for (int i = start; i < end; i++)
            {
                var date = values[i];

                if (date.Month != month)
                {
                    if (mlast >= 0)
                    {
                        if (mlast != mfirst)
                        {
                            titles.Add(((mlast + mfirst) / 2.0, Abbreviations[month - 1]));
                        }

                        labels.Add((i, date.Day.ToString()));
                        breaks.Add(i);
                    }

                    month = date.Month;

                    mfirst = i;
                }

                mlast = i;
            }

            if (mlast >= 0 && mlast != mfirst)
            {
                titles.Add(((mlast + mfirst) / 2.0, Abbreviations[month - 1]));
            }

            if (breaks.Count == 0)
            {
                var j = 1;
                var i = end - 1;
                while (i >= start)
                {
                    if (j++ % delta == 0)
                    {
                        labels.Add((i, values[i].Day.ToString()));
                        breaks.Add(i);
                    }

                    i--;
                }
            }
            else
            {
                var n = breaks.Count;

                var last = (int)breaks[^1];

                var j = 1;
                var i = (int)breaks[0] - 1;
                while (i >= start)
                {
                    if (j++ % delta == 0)
                    {
                        labels.Add((i, values[i].Day.ToString()));
                        breaks.Add(i);
                    }

                    i--;
                }

                for (i = 1; i < n; i++)
                {
                    mfirst = (int)breaks[i - 1];
                    mlast = (int)breaks[i];

                    j = 1;
                    for (var k = mfirst + 1; k < mlast; k++)
                    {
                        if (j % delta == 0 && (mlast - k) >= 2)
                        {
                            labels.Add((k, values[k].Day.ToString()));
                            breaks.Add(k);
                        }

                        j++;
                    }
                }

                j = 1;
                i = last + 1;
                while (i <= end)
                {
                    if (j++ % delta == 0)
                    {
                        labels.Add((i, values[i].Day.ToString()));
                        breaks.Add(i);
                    }

                    i++;
                }
            }

            Breaks = breaks;
            Labels = labels;
            Titles = titles;
        }

        protected void MonthYear(int start, int end)
        {
            var yfirst = -1;
            var ylast = -1;

            var mfirst = -1;
            var mlast = -1;

            var year = 0;
            var month = 0;

            var months = new List<(double x, string month)>();
            var labels = new List<double>();
            var titles = new List<(double x, string year)>();

            for (int i = start; i < end; i++)
            {
                var date = values[i];

                if (date.Year != year)
                {
                    if (ylast >= 0 && ylast != yfirst)
                    {
                        titles.Add(((ylast + yfirst) / 2.0, year.ToString()));
                    }

                    year = date.Year;

                    yfirst = i;
                }

                if (date.Month != month)
                {
                    if (mlast >= 0 && mlast != mfirst)
                    {
                        var label = Abbreviations[month - 1];

                        if (mfirst > 0)
                        {
                            months.Add(((mlast + mfirst) / 2.0, label));
                            labels.Add(mlast);
                        }
                        else
                        {
                            months.Add(((mlast + mfirst) / 2.0, label));

                            labels.Add(mlast);
                        }
                    }

                    month = date.Month;

                    mfirst = i;
                }

                ylast = i;
                mlast = i;
            }

            if (ylast >= 0 && ylast != yfirst)
            {
                titles.Add(((ylast + yfirst) / 2.0, year.ToString()));
            }

            if (mlast >= 0 && mlast != mfirst)
            {
                months.Add(((mlast + mfirst) / 2.0, Abbreviations[month - 1]));
            }

            Breaks = labels;
            Labels = months;
            Titles = titles;
        }

        protected void QuarterYear(int start, int end)
        {
            var yfirst = -1;
            var ylast = -1;

            var mfirst = -1;
            var mlast = -1;

            var year = 0;
            var month = 0;

            var breaks = new List<double>();
            var labels = new List<(double x, string quarter)>();
            var titles = new List<(double x, string year)>();

            for (int i = start; i < end; i++)
            {
                var date = values[i];

                if (date.Year != year)
                {
                    if (ylast >= 0 && ylast != yfirst)
                    {
                        titles.Add(((ylast + yfirst) / 2.0, year.ToString()));
                    }

                    year = date.Year;

                    yfirst = i;
                }

                if (date.Month != month)
                {
                    if (mlast >= 0 && mlast != mfirst)
                    {
                        var label = Abbreviations[month - 1];

                        if (mfirst > 0)
                        {
                            if (month % 3 == 0)
                            {
                                labels.Add((mlast, label));
                                breaks.Add(mlast);
                            }
                        }
                    }

                    month = date.Month;

                    mfirst = i;
                }

                ylast = i;
                mlast = i;
            }

            if (ylast >= 0 && ylast != yfirst)
            {
                titles.Add(((ylast + yfirst) / 2.0, year.ToString()));
            }

            Breaks = breaks;
            Labels = labels;
            Titles = titles;
        }

        protected override void Labeling(int start, int end)
        {
            var n = end - start;

            if (n <= 128)
            {
                DayMonth(start, end);
            }
            else if (n <= 384)
            {
                MonthYear(start, end);
            }
            else
            {
                QuarterYear(start, end);
            }
        }
    }
}
