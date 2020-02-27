using GGNet.Transformations;
using GGNet.Formats;

namespace GGNet.Scales
{
    public class DiscretePosition<T> : Position<T>
        where T : struct
    {
        protected readonly SortedBuffer<T> values = new SortedBuffer<T>(16, 1);

        protected readonly IFormatter<T> formatter;

        public DiscretePosition(ITransformation<T> transformation = null,
            (T? min, T? max)? limits = null,
            (double minMult, double minAdd, double maxMult, double maxAdd)? expand = null,
            IFormatter<T> formatter = null)
            : base(transformation, expand ?? (0.0, 0.6, 0, 0.6))
        {
            Limits = limits ?? (null, null);
            this.formatter = formatter ?? Standard<T>.Instance;
        }

        public override Guide Guide => Guide.None;

        public override void Train(T key) => values.Add(key);

        protected virtual void Labeling(int start, int end)
        {
            var breaks = new double[values.Count];
            var labels = new (double value, string label)[values.Count];

            for (var i = 0; i < values.Count; i++)
            {
                breaks[i] = i;
                labels[i] = (i, formatter.Format(values[i]));
            }

            Breaks = breaks;
            Labels = labels;
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

            Labeling(start, end);
        }

        public override double Map(T key)
        {
            var index = values.IndexOf(key);
            if (index < 0)
            {
                return double.NaN;
            }

            return index;
        }
    }
}
