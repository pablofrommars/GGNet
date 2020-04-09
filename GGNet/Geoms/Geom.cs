using System;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;
using System.Linq;

namespace GGNet.Geoms
{
    public interface IGeom
    {
        Buffer<Shape> Layer { get; }

        void Train();

        void Legend();

        void Shape(bool flip);

        void Clear();
    }

    public abstract class Geom<T, TX, TY> : IGeom
        where TX : struct
        where TY : struct
    {
        protected readonly Source<T> source;
        protected readonly (bool x, bool y) scale;
        protected readonly bool inherit;

        private Facet<T> facet;
        internal Legends legends;

        public Geom(Source<T> source, (bool x, bool y)? scale, bool inherit, Buffer<Shape> layer)
        {
            this.source = source;
            this.scale = scale ?? (true, true);
            this.inherit = inherit;

            Layer = layer ?? new Buffer<Shape>();
        }

        public Buffer<Shape> Layer { get; }

        protected IPositionMapping<T> XMapping<T1, TX1>(Func<T1, TX1> selector, Position<TX1> position)
            where TX1 : struct
        {
            if (typeof(T) != typeof(T1))
            {
                //TODO: throw
            }

            if (typeof(TX1) == typeof(TX))
            {
                return new PositionMapping<T, TX1>(selector as Func<T, TX1>, position);
            }
            else if (typeof(TX1).IsNumeric() && typeof(TX).IsNumeric())
            {
                return new NumericalPositionMapping<T, TX1>(selector as Func<T, TX1>, position as Position<double>);
            }
            else
            {
                return null; //TODO: throw
            }
        }

        protected IPositionMapping<T> XMapping<TX1>(Func<T, TX> selector, Position<TX1> position)
            where TX1 : struct
        {
            if (typeof(TX1) == typeof(TX))
            {
                return new PositionMapping<T, TX>(selector, position as Position<TX>);
            }
            else if (typeof(TX1).IsNumeric() && typeof(TX).IsNumeric())
            {
                return new NumericalPositionMapping<T, TX>(selector, position as Position<double>);
            }
            else
            {
                return null; //TODO: throw
            }
        }

        protected IPositionMapping<T> YMapping<T1, TY1>(Func<T1, TY1> selector, Position<TY1> position)
            where TY1 : struct
        {
            if (typeof(T) != typeof(T1))
            {
                //TODO: throw
            }

            if (typeof(TY1) == typeof(TY))
            {
                return new PositionMapping<T, TY1>(selector as Func<T, TY1>, position);
            }
            else if (typeof(TY1).IsNumeric() && typeof(TX).IsNumeric())
            {
                return new NumericalPositionMapping<T, TY1>(selector as Func<T, TY1>, position as Position<double>);
            }
            else
            {
                return null; //TODO: throw
            }
        }

        protected IPositionMapping<T> YMapping<TY1>(Func<T, TY> selector, Position<TY1> position)
            where TY1 : struct
        {
            if (typeof(TY1) == typeof(TY))
            {
                return new PositionMapping<T, TY>(selector, position as Position<TY>);
            }
            else if (typeof(TY1).IsNumeric() && typeof(TY).IsNumeric())
            {
                return new NumericalPositionMapping<T, TY>(selector, position as Position<double>);
            }
            else
            {
                return null; //TODO: throw
            }
        }

        public virtual void Init<T1, TX1, TY1>(Data<T1, TX1, TY1>.Panel panel, Facet<T1> facet)
            where TX1 : struct
            where TY1 : struct
        {
            if (facet != null && panel.Data.Source.Equals(source))
            {
                this.facet = facet as Facet<T>;
            }

            legends = panel.Data.Legends;
        }

        public abstract void Train(T item);

        public void Train()
        {
            for (int i = 0; i < source.Count; i++)
            {
                var item = source[i];

                if (facet != null && !facet.Include(item))
                {
                    continue;
                }

                Train(item);
            }
        }

        protected void Legend<TV>(IAestheticMapping<T, TV> aes, Func<TV, Elements.IElement> element)
        {
            if (aes == null || !aes.Guide)
            {
                return;
            }

            var legend = legends.GetOrAdd(aes);

            var n = aes.Labels.Count();

            for (int i = 0; i < n; i++)
            {
                var (value, label) = aes.Labels.ElementAt(i);

                legend.Add(label, element(value));
            }
        }

        protected void Legend<TV>(IAestheticMapping<T, TV> aes, Func<TV, Elements.IElement[]> elements)
        {
            if (aes == null || !aes.Guide)
            {
                return;
            }

            var legend = legends.GetOrAdd(aes);

            var n = aes.Labels.Count();

            for (int i = 0; i < n; i++)
            {
                var (value, label) = aes.Labels.ElementAt(i);

                var array = elements(value);

                for (int j = 0; j < array.Length; j++)
                {
                    legend.Add(label, array[j]);
                }
            }
        }

        public virtual void Legend()
        {
        }

        protected abstract void Shape(T item, bool flip);

        protected virtual void Set(bool flip)
        {
        }

        public void Shape(bool flip)
        {
            for (int i = 0; i < source.Count; i++)
            {
                var item = source[i];

                if (facet != null && !facet.Include(item))
                {
                    continue;
                }

                Shape(item, flip);
            }

            Set(flip);
        }

        public virtual void Clear() => Layer?.Clear();
    }
}
