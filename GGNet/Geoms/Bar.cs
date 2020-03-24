using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Bar<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private class Comparer : IComparer<(double x, Buffer<(string fill, double value)> y)>
        {
            public int Compare([AllowNull] (double x, Buffer<(string fill, double value)> y) x, [AllowNull] (double x, Buffer<(string fill, double value)> y) y) => x.x.CompareTo(y.x);

            public static readonly Comparer Instance = new Comparer();
        }

        private readonly SortedBuffer<(double x, Buffer<(string fill, double value)> y)> bars = new SortedBuffer<(double x, Buffer<(string fill, double value)> y)>(32, 1, Comparer.Instance);

        private readonly PositionAdjustment position;
        private readonly double width;

        public Bar(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            IAestheticMapping<T, string> fill = null,
            PositionAdjustment position = PositionAdjustment.Stack,
            double width = 0.9,
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

            this.position = position;
            this.width = width;
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

            var exist = false;

            if (flip)
            {
                for (var i = 0; i < bars.Count; i++)
                {
                    var bar = bars[i];
                    if (bar.x == y)
                    {
                        bar.y.Add((fill, x));
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                {
                    var bar = new Buffer<(string fill, double value)>(8, 1);
                    bar.Add((fill, x));
                    bars.Add((y, bar));
                }
            }
            else
            {
                for (var i = 0; i < bars.Count; i++)
                {
                    var bar = bars[i];
                    if (bar.x == x)
                    {
                        bar.y.Add((fill, y));
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                {
                    var bar = new Buffer<(string fill, double value)>(8, 1);
                    bar.Add((fill, y));
                    bars.Add((x, bar));
                }
            }
        }

        private void Stack(bool flip)
        {
            var delta = width;

            if (bars.Count > 1)
            {
                var d = double.MaxValue;

                for (var i = 1; i < bars.Count; i++)
                {
                    d = Math.Min(d, bars[i].x - bars[i - 1].x);
                }

                delta *= d;
            }

            if (flip)
            {
                for (var i = 0; i < bars.Count; i++)
                {
                    var bar = bars[i];
                    var sum = 0.0;

                    for (var j = bar.y.Count - 1; j >= 0; j--)
                    {
                        var y = bar.y[j];

                        Layer.Add(new Rectangle
                        {
                            X = sum,
                            Y = bar.x - delta / 2.0,
                            Width = sum + y.value,
                            Height = delta,
                            Aesthetic = new Elements.Rectangle
                            {
                                Fill = y.fill,
                                Alpha = Aesthetic.Alpha
                            }
                        });

                        sum += y.value;
                    }

                    Positions.X.Position.Shape(0, sum);
                    Positions.Y.Position.Shape(bar.x - delta, bar.x + delta);
                }
            }
            else
            {
                for (var i = 0; i < bars.Count; i++)
                {
                    var bar = bars[i];
                    var sum = 0.0;

                    for (var j = bar.y.Count - 1; j >= 0; j--)
                    {
                        var y = bar.y[j];

                        Layer.Add(new Rectangle
                        {
                            X = bar.x - delta / 2.0,
                            Y = sum,
                            Width = delta,
                            Height = sum + y.value,
                            Aesthetic = new Elements.Rectangle
                            {
                                Fill = y.fill,
                                Alpha = Aesthetic.Alpha
                            }
                        });

                        sum += y.value;
                    }

                    Positions.X.Position.Shape(bar.x - delta, bar.x + delta);
                    Positions.Y.Position.Shape(0, sum);
                }
            }
        }

        private void Dodge(bool flip)
        {
            if (flip)
            {
                throw new NotImplementedException();
            }
            else
            {
                var delta = width;

                if (bars.Count > 1)
                {
                    var d = double.MaxValue;

                    for (var i = 1; i < bars.Count; i++)
                    {
                        d = Math.Min(d, bars[i].x - bars[i - 1].x);
                    }

                    delta *= d;
                }

                for (var i = 0; i < bars.Count; i++)
                {
                    var bar = bars[i];
                    var n = bar.y.Count;

                    var w = delta / n;
                    var x = bar.x - delta / 2.0;

                    for (var j = 0; j < n; j++)
                    {
                        var (fill, value) = bar.y[j];

                        Layer.Add(new Rectangle
                        {
                            X = x,
                            Y = value >= 0 ? 0 : value,
                            Width = w,
                            Height = Math.Abs(value),
                            Aesthetic = new Elements.Rectangle
                            {
                                Fill = fill,
                                Alpha = Aesthetic.Alpha
                            }
                        });

                        Positions.X.Position.Shape(x, x + w);
                        if (value >= 0)
                        {
                            Positions.Y.Position.Shape(0, value);
                        }
                        else
                        {
                            Positions.Y.Position.Shape(value, 0);
                        }

                        x += w;
                    }
                }
            }
        }

        protected override void Set(bool flip)
        {
            if (position == PositionAdjustment.Stack)
            {
                Stack(flip);
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
    }
}
