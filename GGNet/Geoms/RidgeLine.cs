using System;
using System.Collections.Generic;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class RidgeLine<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private readonly Dictionary<(double y, string fille), Area> areas = new Dictionary<(double y, string fille), Area>();

        public RidgeLine(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            Func<T, double> height,
            IAestheticMapping<T, string> fill = null,
            (bool x, bool y)? scale = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, scale, inherit, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                Height = height
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

            public Func<T, double> Height { get; set; }
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
            Area area;

            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);

            if (Aesthetics.Fill == null)
            {
                var key = (y, Aesthetic.Fill);

                if (!areas.TryGetValue(key, out area))
                {
                    area = new Area { Aesthetic = Aesthetic };

                    Layer.Add(area);

                    areas[key] = area;
                }
            }
            else
            {
                var fill = Aesthetics.Fill.Map(item);
                if (string.IsNullOrEmpty(fill))
                {
                    return;
                }

                var key = (y, fill);

                if (!areas.TryGetValue(key, out area))
                {
                    area = new Area
                    {
                        Aesthetic = new Elements.Rectangle
                        {
                            Fill = fill,
                            Alpha = Aesthetic.Alpha
                        }
                    };

                    Layer.Add(area);

                    areas[key] = area;
                }
            }

            var height = Selectors.Height(item);

            area.Points.Add((x, y, y + height));

            if (scale.x)
            {
                Positions.X.Position.Shape(x, x);
            }

            if (scale.y)
            {
                Positions.Y.Position.Shape(y, y + height);
            }
        }

        public override void Clear()
        {
            base.Clear();

            areas.Clear();
        }
    }
}
