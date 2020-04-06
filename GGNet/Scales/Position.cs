using System;

using GGNet.Transformations;

using static System.Math;

namespace GGNet.Scales
{
    public interface IPosition : IScale
    {
        void Shape(double min, double max);

        double Coord(double value);
    }

    public abstract class Position<TKey> : Continuous<TKey>, IPosition
        where TKey : struct
    {
        protected readonly (double minMult, double minAdd, double maxMult, double maxAdd) expand;

        public Position(ITransformation<TKey> transformation, (double minMult, double minAdd, double maxMult, double maxAdd) expand)
               : base(transformation)
        {
            this.expand = expand;
        }

        public override void Train(TKey key) { }

        public (double min, double max) Range { get; protected set; }

        public virtual ITransformation<double> RangeTransformation { get; } = Transformations.Identity<double>.Instance;

        protected void SetRange(double min, double max)
        {
            if (min == max)
            {
                Range = (
                    min - 0.05,
                    max + 0.05
                );
            }
            else
            {
                if (max < min)
                {
                    var tmp = min;
                    min = max;
                    max = tmp;
                }

                var range = max - min;

                Range = (
                    min - (expand.minMult * range + expand.minAdd),
                    max + expand.maxMult * range + expand.maxAdd
                );
            }
        }

        protected double? _min = null;
        protected double? _max = null;

        public virtual void Shape(double min, double max)
        {
            _min ??= min;
            _max ??= max;

            _min = Min(_min.Value, min);
            _max = Max(_max.Value, max);
        }

        public virtual double Coord(double value)
        {
            if (Range.min == Range.max)
            {
                return 0;
            }

            return (value - Range.min) / (Range.max - Range.min);
        }

        public override void Clear()
        {
            _min = null;
            _max = null;
        }
    }

    public interface IPositionMapping<T>
    {
        IPosition Position { get; }

        void Train(T item);

        double Map(T item);
    }

    public class PositionMapping<T, TKey> : IPositionMapping<T>
        where TKey : struct
    {
        private readonly Func<T, TKey> selector;
        private readonly Position<TKey> position;

        public PositionMapping(Func<T, TKey> selector, Position<TKey> position)
        {
            this.selector = selector;
            this.position = position;
        }

        public IPosition Position => position;

        public void Train(T item) => position.Train(selector(item));

        public double Map(T item) => position.Map(selector(item));
    }

    public class NumericalPositionMapping<T, TKey> : IPositionMapping<T>
    {
        private readonly Func<T, TKey> selector;
        private readonly Position<double> scale;

        public NumericalPositionMapping(Func<T, TKey> selector, Position<double> scale)
        {
            this.selector = selector;
            this.scale = scale;
        }

        public IPosition Position => scale;

        public void Train(T item) => scale.Train(Convert<TKey>.ToDouble(selector(item)));

        public double Map(T item) => scale.Map(Convert<TKey>.ToDouble(selector(item)));
    }
}
