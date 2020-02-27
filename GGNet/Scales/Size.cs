using GGNet.Transformations;

using static System.Math;

namespace GGNet.Scales
{
    public class SizeContinuous : Continuous<double>
    {
        protected readonly bool defined;
        protected (double min, double max) limits;
        protected readonly (double min, double max) range;
        protected readonly bool oob;
        private readonly string format;

        public SizeContinuous(
            (double min, double max)? limits = null,
            (double min, double max)? range = null,
            bool oob = false,
            string format = "0.##",
            ITransformation<double> transformation = null)
            : base(transformation)
        {
            if (limits.HasValue)
            {
                defined = true;
                this.limits = limits.Value;
            }
            else
            {
                this.limits = (0.0, 0.0);
            }

            this.range = range ?? (0.0, 1.0);
            this.oob = oob;

            this.format = format;
        }

        public override Guide Guide => Guide.Items;

        public override void Train(double key)
        {
            if (defined)
            {
                return;
            }

            if (limits.min == 0 && limits.max == 0)
            {
                limits = (key, key);
            }
            else
            {
                limits = (Min(limits.min, key), Max(limits.max, key));
            }
        }

        public override void Set()
        {
            var breaks = Wilkinson.extended(limits.min, limits.max);

            var labels = new (double value, string label)[breaks.Length];

            for (var i = 0; i < breaks.Length; i++)
            {
                var value = breaks[i];

                labels[i] = (Map(value), value.ToString(format));
            }

            Breaks = breaks;
            Labels = labels;
        }

        public override double Map(double key)
        {
            if (!oob)
            {
                if (key < limits.min || key > limits.max)
                {
                    return double.NaN;
                }
            }

            if (key < limits.min)
            {
                return range.min;
            }
            else if (key > limits.max)
            {
                return range.max;
            }

            if (limits.min >= 0)
            {
                return range.min + (Sqrt(key) - Sqrt(limits.min)) / (Sqrt(limits.max) - Sqrt(limits.min)) * (range.max - range.min);
            }
            else
            {
                return range.min + Sqrt(key - limits.min) / Sqrt(limits.max - limits.min) * (range.max - range.min);
            }
        }

        public override void Clear()
        {
            if (defined)
            {
                return;
            }

            limits = (0.0, 0.0);
        }
    }
}
