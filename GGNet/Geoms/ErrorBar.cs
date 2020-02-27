using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class ErrorBar<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private class Comparer : IComparer<(double x, Buffer<(string color, double y, double ymin, double ymax)> bars)>
        {
            public int Compare([AllowNull] (double x, Buffer<(string color, double y, double ymin, double ymax)> bars) x, [AllowNull] (double x, Buffer<(string color, double y, double ymin, double ymax)> bars) y) => x.x.CompareTo(y.x);

            public static readonly Comparer Instance = new Comparer();
        }

        private readonly SortedBuffer<(double x, Buffer<(string color, double y, double ymin, double ymax)> bars)> bars = new SortedBuffer<(double x, Buffer<(string color, double y, double ymin, double ymax)> bars)>(32, 1, Comparer.Instance); 

        private readonly PositionAdjustment position;

        public ErrorBar(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            Func<T, TY> ymin,
            Func<T, TY> ymax,
            IAestheticMapping<T, string> color = null,
            PositionAdjustment position = PositionAdjustment.Identity,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, inherit, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                YMin = ymin,
                YMax = ymax
            };

            Aesthetics = new _Aesthetics
            {
                Color = color
            };

            this.position = position;
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, TY> YMin { get; set; }

            public Func<T, TY> YMax { get; set; }
        }

        public _Selectors Selectors { get; }

        public class _Aesthetics
        {
            public IAestheticMapping<T, string> Color { get; set; }
        }

        public _Aesthetics Aesthetics { get; }

        public class _Positions
        {
            public IPositionMapping<T> X { get; set; }

            public IPositionMapping<T> Y { get; set; }

            public IPositionMapping<T> YMin { get; set; }

            public IPositionMapping<T> YMax { get; set; }
        }

        public _Positions Positions { get; } = new _Positions();

        public Elements.Line Line { get; set; }

        public Elements.Circle Circle { get; set; }

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

            Positions.YMin = YMapping(Selectors.YMin, panel.Y);

            Positions.YMax = YMapping(Selectors.YMax, panel.Y);

            if (!inherit)
            {
                return;
            }

            Aesthetics.Color ??= panel.Data.Aesthetics.Color as IAestheticMapping<T, string>;
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
            Positions.Y.Train(item);
            Positions.YMin.Train(item);
            Positions.YMax.Train(item);

            Aesthetics.Color?.Train(item);
        }

        public override void Legend()
        {
            Legend(Aesthetics.Color, value => new Elements.IElement[]
            {
                new Elements.VLine
                {
                    Width = Line.Width,
                    Fill = value,
                    Alpha = Line.Alpha,
                    LineType = Line.LineType
                },
                new Elements.Circle
                {
                    Fill = value,
                    Alpha = Circle.Alpha,
                    //Radius = Circle.Radius
                    Radius = 2
                }
            });
        }

        protected override void Shape(T item, bool flip)
        {
            var color = Line.Fill;

            if (Aesthetics.Color != null)
            {
                color = Aesthetics.Color.Map(item);
                if (string.IsNullOrEmpty(color))
                {
                    return;
                }
            }

            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);
            var ymin = Positions.YMin.Map(item);
            var ymax = Positions.YMax.Map(item);

            var exist = false;

            for (var i = 0; i < bars.Count; i++)
            {
                var bar = bars[i];
                if (bar.x == x)
                {
                    bar.bars.Add((color, y, ymin, ymax));
                    exist = true;
                    break;
                }
            }

            if (!exist)
            {
                var bar = new Buffer<(string color, double y, double ymin, double ymax)>(8, 1);
                bar.Add((color, y, ymin, ymax));
                bars.Add((x, bar));
            }
        }

        private void Identity(bool flip)
        {
            for (var i = 0; i < bars.Count; i++)
            {
                var bar = bars[i];

                for (var j = 0; j < bar.bars.Count; j++)
                {
                    var (color, y, ymin, ymax) = bar.bars[j];

                    Layer.Add(new Line
                    {
                        X1 = bar.x,
                        X2 = bar.x,
                        Y1 = ymin,
                        Y2 = ymax,
                        Aesthetic = new Elements.Line
                        {
                            Width = Line.Width,
                            Fill = color,
                            Alpha = Line.Alpha,
                            LineType = Line.LineType
                        }
                    });

                    Layer.Add(new Circle
                    {
                        X = bar.x,
                        Y = y,
                        Aesthetic = new Elements.Circle
                        {
                            Radius = Circle.Radius,
                            Fill = color,
                            Alpha = Circle.Alpha
                        },
                    });

                    Positions.X.Position.Shape(bar.x, bar.x);
                    Positions.YMin.Position.Shape(ymin, ymax);
                    Positions.YMax.Position.Shape(ymin, ymax);
                }
            }
        }

        private void Dodge(bool flip)
        {

            var delta = 0.6;

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
                var n = bar.bars.Count;

                var w = delta / n;
                var x = bar.x - delta / 2.0 + w / 2.0;

                for (var j = 0; j < n; j++)
                {
                    var (color, y, ymin, ymax) = bar.bars[j];

                    Layer.Add(new Line
                    {
                        X1 = x,
                        X2 = x,
                        Y1 = ymin,
                        Y2 = ymax,
                        Aesthetic = new Elements.Line
                        {
                            Width = Line.Width,
                            Fill = color,
                            Alpha = Line.Alpha,
                            LineType = Line.LineType
                        }
                    });

                    Layer.Add(new Circle
                    {
                        X = x,
                        Y = y,
                        Aesthetic = new Elements.Circle
                        {
                            Radius = Circle.Radius,
                            Fill = color,
                            Alpha = Circle.Alpha
                        },
                    });

                    Positions.X.Position.Shape(x, x);
                    Positions.YMin.Position.Shape(ymin, ymax);
                    Positions.YMax.Position.Shape(ymin, ymax);

                    x += w;
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
    }
}
