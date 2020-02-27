using System;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Map<T> : Geom<T, double, double>
    {
        public Map(
            Source<T> source,
            Func<T, double[]> latitude,
            Func<T, double[]> longitude,
            IAestheticMapping<T, string> fill = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, inherit, layer)
        {
            Selectors = new _Selectors
            {
                Latitude = latitude,
                Longitude = longitude,
            };

            Aesthetics = new _Aesthetics
            {
                Fill = fill
            };
        }

        public class _Selectors
        {
            public Func<T, double[]> Latitude { get; set; }

            public Func<T, double[]> Longitude { get; set; }
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
        }

        public _Positions Positions { get; } = new _Positions();

        public Elements.Rectangle Aesthetic { get; set; }

        public override void Init<T1, TX1, TY1>(Data<T1, TX1, TY1>.Panel panel, Facet<T1> facet)
        {
            base.Init(panel, facet);

            Positions.X = XMapping(panel.Data.Selectors.X, panel.X);
            Positions.Y = YMapping(panel.Data.Selectors.Y, panel.Y);

            if (!inherit)
            {
                return;
            }

            Aesthetics.Fill ??= panel.Data.Aesthetics.Fill as IAestheticMapping<T, string>;
        }

        public override void Train(T item)
        {
            Aesthetics.Fill?.Train(item);
        }

        public override void Legend()
        {
            Legend(Aesthetics.Fill, value => new Elements.Rectangle
            {
                Fill = value,
                Alpha = Aesthetic.Alpha,
                Color = Aesthetic.Color,
                Width = Aesthetic.Width
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

            var latitudes = Selectors.Latitude(item);
            var longitudes = Selectors.Longitude(item);

            var xmin = double.MaxValue;
            var xmax = double.MinValue;

            var ymin = double.MaxValue;
            var ymax = double.MinValue;

            var poly = new Polygon
            {
                Aesthetic = new Elements.Rectangle
                {
                    Fill = fill,
                    Alpha = Aesthetic.Alpha,
                    Color = Aesthetic.Color,
                    Width = Aesthetic.Width
                }
            };

            for (var i = 0; i < latitudes.Length; i++)
            {
                var x = latitudes[i];
                var y = longitudes[i];

                if (x < xmin)
                {
                    xmin = x;
                }

                if (x > xmax)
                {
                    xmax = x;
                }

                if (y < ymin)
                {
                    ymin = y;
                }

                if (y > ymax)
                {
                    ymax = y;
                }

                poly.Points.Add((x, y));
            }

            Layer.Add(poly);

            Positions.X.Position.Shape(xmin, xmax);
            Positions.Y.Position.Shape(ymin, ymax);
        }
    }
}
