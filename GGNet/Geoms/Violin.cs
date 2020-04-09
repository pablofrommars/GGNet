using System;
using System.Collections.Generic;
using System.Linq;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Violin<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private class Comparer : IComparer<(double y, double width)>
        {
            public int Compare((double y, double width) a, (double y, double width) b) => a.y.CompareTo(b.y);

            public static readonly Comparer Instance = new Comparer();
        }

        private readonly SortedDictionary<double, Dictionary<string, SortedBuffer<(double y, double width)>>> violins = new SortedDictionary<double, Dictionary<string, SortedBuffer<(double y, double width)>>>();

        private readonly PositionAdjustment position;

        public Violin(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            Func<T, double> width,
            IAestheticMapping<T, string> fill = null,
            PositionAdjustment position = PositionAdjustment.Identity,
            (bool x, bool y)? scale = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, scale, inherit, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                Width = width
            };

            Aesthetics = new _Aesthetics
            {
                Fill = fill
            };

            this.position = position;
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, double> Width { get; set; }
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

            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);

            SortedBuffer<(double y, double width)> violin;

            if (violins.TryGetValue(x, out var xviolins))
            {
                if (!xviolins.TryGetValue(fill, out violin))
                {
                    violin = new SortedBuffer<(double y, double width)>(64, 1, Comparer.Instance);
                    xviolins[fill] = violin;
                }
            }
            else
            {
                violin = new SortedBuffer<(double y, double width)>(64, 1, Comparer.Instance);

                violins[x] = new Dictionary<string, SortedBuffer<(double y, double width)>>
                {
                    [fill] = violin
                };
            }

            violin.Add((y, Selectors.Width(item)));
        }

        private void Identity(bool flip)
        {
            foreach (var xviolins in violins)
            {
                var c = xviolins.Key;

                foreach (var violin in xviolins.Value)
                {
                    var xmin = c;
                    var xmax = c;

                    var n = violin.Value.Count;
                    var longitude = new double[2 * n];
                    var latitude = new double[2 * n];
                    var j = 0;

                    for (var i = 0; i < n; i++)
                    {
                        var (y, width) = violin.Value[i];

                        var x = c - 0.45 * width;
                        if (x < xmin)
                        {
                            xmin = x;
                        }

                        longitude[j] = x;
                        latitude[j] = y;

                        j++;
                    }

                    for (var i = n - 1; i >= 0; i--)
                    {
                        var (y, width) = violin.Value[i];

                        var x = c + 0.45 * width;
                        if (xmax < x)
                        {
                            xmax = x;
                        }

                        longitude[j] = x;
                        latitude[j] = y;

                        j++;
                    }

                    var poly = new Polygon
                    {
                        Path = new Geospacial.Polygon
                        {
                            Longitude = longitude,
                            Latitude = latitude
                        },
                        Aesthetic = new Elements.Rectangle
                        {
                            Fill = violin.Key,
                            Alpha = Aesthetic.Alpha,
                            Color = Aesthetic.Color,
                            Width = Aesthetic.Width
                        }
                    };

                    Layer.Add(poly);

                    Positions.X.Position.Shape(xmin, xmax);
                    Positions.Y.Position.Shape(violin.Value[0].y, violin.Value[n - 1].y);
                }
            }
        }

        private void Dodge(bool flip)
        {
            var delta = 0.8;

            if (violins.Count > 1)
            {
                var d = double.MaxValue;

                for (var i = 1; i < violins.Count; i++)
                {
                    d = Math.Min(d, violins.ElementAt(i).Key - violins.ElementAt(i - 1).Key);
                }

                delta *= d;
            }

            foreach (var xviolins in violins)
            {
                var n = xviolins.Value.Count;

                var w = delta / n;
                var c = xviolins.Key - delta / 2.0 + w / 2.0;

                foreach (var violin in xviolins.Value)
                {
                    var xmin = c;
                    var xmax = c;

                    var N = violin.Value.Count;
                    var longitude = new double[2 * N];
                    var latitude = new double[2 * N];
                    var j = 0;

                    for (var i = 0; i < N; i++)
                    {
                        var (y, width) = violin.Value[i];

                        var x = c - 0.4 * w * width;
                        if (x < xmin)
                        {
                            xmin = x;
                        }

                        longitude[j] = x;
                        latitude[j] = y;

                        j++;
                    }

                    for (var i = N - 1; i >= 0; i--)
                    {
                        var (y, width) = violin.Value[i];

                        var x = c + 0.4 * w * width;
                        if (xmax < x)
                        {
                            xmax = x;
                        }

                        longitude[j] = x;
                        latitude[j] = y;

                        j++;
                    }

                    var poly = new Polygon
                    {
                        Path = new Geospacial.Polygon
                        {
                            Longitude = longitude,
                            Latitude = latitude
                        },
                        Aesthetic = new Elements.Rectangle
                        {
                            Fill = violin.Key,
                            Alpha = Aesthetic.Alpha,
                            Color = Aesthetic.Color,
                            Width = Aesthetic.Width
                        }
                    };

                    Layer.Add(poly);

                    if (scale.x)
                    {
                        Positions.X.Position.Shape(xmin, xmax);
                    }

                    if (scale.y)
                    {
                        Positions.Y.Position.Shape(violin.Value[0].y, violin.Value[N - 1].y);
                    }

                    c += w;
                }
            }
        }

        protected override void Set(bool flip)
        {
            if (position == PositionAdjustment.Identity)
            {
                Identity(flip);
            }
            else if (position == PositionAdjustment.Dodge)
            {
                Dodge(flip);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void Clear()
        {
            base.Clear();

            violins.Clear();
        }
    }
}
