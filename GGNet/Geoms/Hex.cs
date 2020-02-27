using System;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Hex<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        public Hex(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            Func<T, TX> Dx,
            Func<T, TY> Dy,
            IAestheticMapping<T, string> fill = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, inherit, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                Dx = Dx,
                Dy = Dy
            };

            Aesthetics = new _Aesthetics
            {
                Fill = fill
            };
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, TX> Dx { get; set; }

            public Func<T, TY> Dy { get; set; }
        }

        public _Selectors Selectors { get; }

        public class _Aesthetics
        {
            public IAestheticMapping<T, string> Fill { get; set; }
        }

        public _Aesthetics Aesthetics { get; }

        public class _Positions
        {
            public IPositionMapping<T> X { get; set; }

            public IPositionMapping<T> Y { get; set; }

            public IPositionMapping<T> Dx { get; set; }

            public IPositionMapping<T> Dy { get; set; }
        }

        public _Positions Positions { get; } = new _Positions();

        public Elements.Rectangle Aesthetic { get; set; }

        public override void Init<T1, TX1, TY1>(Data<T1, TX1, TY1>.Panel panel, Facet<T1> facet)
        {
            base.Init(panel, facet);

            if (Selectors.X == null)
            {
                Positions.X = XMapping(panel.Data.Selectors.X, panel.X);
            }
            else
            {
                Positions.X = XMapping(Selectors.X, panel.X);
            }

            if (Selectors.Y == null)
            {
                Positions.Y = YMapping(panel.Data.Selectors.Y, panel.Y);
            }
            else
            {
                Positions.Y = YMapping(Selectors.Y, panel.Y);
            }

            Positions.Dx = XMapping(Selectors.Dx, panel.X);
            Positions.Dy = YMapping(Selectors.Dy, panel.Y);

            if (!inherit)
            {
                return;
            }

            Aesthetics.Fill ??= panel.Data.Aesthetics.Fill as IAestheticMapping<T, string>;
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
            Positions.Y.Train(item);
            Positions.Dx.Train(item);
            Positions.Dy.Train(item);

            Aesthetics.Fill?.Train(item);
        }

        public override void Legend()
        {
            Legend(Aesthetics.Fill, value => new Elements.Rectangle
            {
                Fill = value,
                Alpha = Aesthetic.Alpha
            });
        }

        protected override void Shape(T item, bool flip)
        {
            var fill = Aesthetic.Fill;

            if (Aesthetics.Fill != null)
            {
                fill = Aesthetics.Fill.Map(item);
                if (string.IsNullOrEmpty(fill))
                {
                    return;
                }
            }

            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);
            var dx = Positions.Dx.Map(item) / 2.0;
            var dy = Positions.Dy.Map(item) / Math.Sqrt(3.0) / 2.0 * 1.15;

            var hex = new Polygon
            {
                Aesthetic = new Elements.Rectangle
                {
                    Fill = fill,
                    Alpha = Aesthetic.Alpha
                }
            };

            Layer.Add(hex);

            hex.Points.Add((x + dx, y + dy));
            hex.Points.Add((x + dx, y - dy));
            hex.Points.Add((x, y - 2.0 * dy));
            hex.Points.Add((x - dx, y - dy));
            hex.Points.Add((x - dx, y + dy));
            hex.Points.Add((x, y + 2 * dy));

            Positions.X.Position.Shape(x - dx, x + dx);
            Positions.Y.Position.Shape(y - 2.0 * dy, y + 2.0 * dy);
        }
    }
}
