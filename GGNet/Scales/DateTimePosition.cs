using System.Collections.Generic;

using NodaTime;
using NodaTime.Text;

using GGNet.Transformations;

namespace GGNet.Scales
{
    public class DateTimePosition : Position<LocalDateTime>
    {
        private static readonly LocalTimePattern timePattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm");
        private static readonly LocalDatePattern datePattern = LocalDatePattern.CreateWithInvariantCulture("MM/dd");
        private static readonly Period sampling = Period.FromMinutes(1);

        protected readonly SortedBuffer<LocalDateTime> values = new SortedBuffer<LocalDateTime>(512, 1);

        public DateTimePosition(ITransformation<LocalDateTime> transformation = null,
            (LocalDateTime? min, LocalDateTime? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null)
            : base(transformation, expand ?? (0.0, 5, 0, 5))
        {
            Limits = limits ?? (null, null);
        }

        private LocalDate? first = null;
        private LocalDate? last = null;

        private LocalDateTime? min = null;
        private LocalDateTime? max = null;

        public override void Train(LocalDateTime key)
        {
            if (values.Count > 0)
            {
                var date = key.Date;
                if (first > date)
                {
                    first = date;
                }

                date = key.Date;
                if (last < date)
                {
                    last = date;
                }

                if (min > key)
                {
                    min = key;
                }

                if (max < key)
                {
                    max = key;
                }

                var current = values[values.Count - 1];
                if (current.Date == key.Date)
                {
                    current = current.Plus(sampling);

                    while(current <= key)
                    {
                        values.Add(current);

                        current = current.Plus(sampling);
                    }
                }
                else
                {
                    values.Add(key);
                }
            }
            else
            {
                values.Add(key);

                max = key;
                first = key.Date;
                last = key.Date;
                min = key;
                max = key;
            }
        }

        public override void Set()
        {
            var min = _min ?? 0.0;
            var max = _max ?? 0.0;

            var start = 0;
            var end = values.Count;

            if (Limits.min.HasValue)
            {
                var index = values.IndexOf(Limits.min.Value);
                if (index >= 0)
                {
                    min = index;
                    start = index;
                }
            }

            if (Limits.max.HasValue)
            {
                var index = values.IndexOf(Limits.max.Value);
                if (index >= 0)
                {
                    max = index;
                    end = index + 1;
                }
            }

            SetRange(min, max);

            var breaks = new List<double>();
            var minor = new List<double>();
            var labels = new List<(double x, string label)>();
            var titles = new List<(double x, string title)>();

            var dfirst = -1;
            var dlast = -1;

            var day = new LocalDate();

            for (int i = start; i < end; i++)
            {
                var date = values[i];

                if (date.Date != day)
                {
                    if (dlast >= 0 && dlast != dfirst)
                    {
                        titles.Add(((dlast + dfirst) / 2.0, datePattern.Format(day)));
                    }

                    day = date.Date;

                    dfirst = i;
                }

                if (date.Minute % 30 == 0)
                {
                    breaks.Add(i);
                    labels.Add((i, timePattern.Format(date.TimeOfDay)));
                }
                else if (date.Minute % 15 == 0)
                {
                    minor.Add(i);
                }

                values.Add(date);

                dlast = i;
            }

            if (dlast >= 0 && dlast != dfirst)
            {
                titles.Add(((dlast + dfirst) / 2.0, datePattern.Format(day)));
            }

            Breaks = breaks;
            MinorBreaks = minor;
            Labels = labels;
            Titles = titles;
        }

        public override double Map(LocalDateTime key)
        {
            var index = values.IndexOf(key);
            if (index < 0)
            {
                return double.NaN;
            }

            return index;
        }

        public override void Clear()
        {
            base.Clear();

            first = null;
            last = null;

            min = null;
            max = null;

            values.Clear();
        }
    }
}
