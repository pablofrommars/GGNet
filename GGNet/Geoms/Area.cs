using System;
using System.Collections.Generic;
using System.Linq;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Area<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private readonly Dictionary<object, Area> areas = new Dictionary<object, Area>();

        public Area(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            IAestheticMapping<T, string> fill = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, inherit, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y
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
            if (Aesthetics.Fill != null && Aesthetics.Fill.Guide)
            {
                var legend = legends.GetOrAdd(Aesthetics.Fill);

                var labels = Aesthetics.Fill.Labels;

                var n = labels.Count();

                for (int i = 0; i < n; i++)
                {
                    var (value, label) = labels.ElementAt(i);

                    legend.Add(label, new Elements.Rectangle
                    {
                        Fill = value,
                        Alpha = Aesthetic.Alpha
                    });
                }
            }
        }

        private Area _area = null;

        protected override void Shape(T item, bool flip)
        {
            Area area;

            if (Aesthetics.Fill == null)
            {
                if (_area == null)
                {
                    _area = new Area { Aesthetic = Aesthetic };

                    Layer.Add(_area);
                }

                area = _area;
            }
            else
            {
                var fill = Aesthetics.Fill.Map(item);
                if (string.IsNullOrEmpty(fill))
                {
                    return;
                }

                if (!areas.TryGetValue(fill, out area))
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

                    areas[fill] = area;
                }
            }

            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);

            area.Points.Add((x, 0, y));

            Positions.X.Position.Shape(x, x);
            Positions.Y.Position.Shape(y, y);
        }
    }
}
