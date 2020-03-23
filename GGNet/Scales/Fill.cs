using GGNet.Transformations;

using static System.Math;

namespace GGNet.Scales
{
    public class FillDiscrete<TKey> : Discrete<TKey, string>
    {
        public FillDiscrete(
           Palettes.Discrete<TKey, string> palette,
            ITransformation<TKey> transformation = null)
           : base(palette, default, transformation)
        {
        }

        public FillDiscrete(
            string[] palette,
            int direction = 1,
            ITransformation<TKey> transformation = null)
            : base(palette, direction, default, transformation)
        {
        }

        public override Guide Guide => Guide.Items;
    }

    public class FillContinuous : Scale<double, string>
    {
        private readonly string[] colors;
        private readonly int m;
        private readonly string format;

        protected (double min, double max) limits = (0.0, 0.0);

        public FillContinuous(
            string[] colors,
            int m = 5,
            string format = "0.##")
            : base()
        {
            this.colors = colors;
            this.m = m;
            this.format = format;
        }

        public override Guide Guide => Guide.ColorBar;

        public override void Train(double key)
        {
            if (limits.min == 0 && limits.max == 0)
            {
                limits = (key, key);
            }
            else
            {
                limits = (Min(limits.min, key), Max(limits.max, key));
            }
        }

        public override void Set(bool grid)
        {
            if (!grid)
            {
                return;
            }

            var extended = Wilkinson.extended(limits.min, limits.max, m);

            var labels = new (string value, string label)[extended.Length];
            var breaks = new string[extended.Length];

            for (var i = 0; i < extended.Length; i++)
            {
                var value = extended[extended.Length - i - 1];
                var mapped = Map(value);

                breaks[i] = mapped;
                labels[i] = (mapped, value.ToString(format));
            }

            Breaks = breaks;
            Labels = labels;
        }

        public override string Map(double key)
        {
            if (limits.max > limits.min)
            {
                return colors[Max(Min((int)(((key - limits.min) / (limits.max - limits.min)) * (colors.Length - 1)), colors.Length - 1), 0)];
            }
            else
            {
                return string.Empty;
            }
        }

        public override void Clear()
        {
            limits = (0.0, 0.0);
        }
    }
}

