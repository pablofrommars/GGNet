using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Bar<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private class Comparer : IComparer<(double x, Buffer<(T item, string fill, double value)> y)>
        {
            public int Compare((double x, Buffer<(T item, string fill, double value)> y) x, (double x, Buffer<(T item, string fill, double value)> y) y) => x.x.CompareTo(y.x);

            public static readonly Comparer Instance = new Comparer();
        }

        private readonly SortedBuffer<(double x, Buffer<(T item, string fill, double value)> y)> bars = new SortedBuffer<(double x, Buffer<(T item, string fill, double value)> y)>(32, 1, Comparer.Instance);

        private readonly PositionAdjustment position;
        private readonly double width;

        private readonly bool animation;

        public Bar(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            IAestheticMapping<T, string> fill = null,
            Func<T, string> tooltip = null,
            PositionAdjustment position = PositionAdjustment.Stack,
            double width = 0.9,
            bool animation = false,
            (bool x, bool y)? scale = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, scale, inherit, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                Tooltip = tooltip
            };

            Aesthetics = new _Aesthetics
            {
                Fill = fill
            };

            this.position = position;
            this.width = width;

            this.animation = animation;
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, string> Tooltip { get; set; }
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

        public Func<T, MouseEventArgs, Task> OnClick { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOver { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOut { get; set; }

        private Func<T, double, double, MouseEventArgs, Task> onMouseOver;

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

            if (OnMouseOver == null && OnMouseOut == null && Selectors.Tooltip != null)
            {
                onMouseOver = (item, x, y, _) =>
                {
                    panel.Component.Tooltip.Show(
                        x,
                        y,
                        0,
                        Selectors.Tooltip(item),
                        Aesthetics.Fill?.Map(item) ?? Aesthetic.Fill,
                        Aesthetic.Alpha);

                    return Task.CompletedTask;
                };

                OnMouseOut = (_, __) =>
                {
                    panel.Component.Tooltip.Hide();

                    return Task.CompletedTask;
                };
            }
            else if (OnMouseOver != null)
            {
                onMouseOver = (item, _, __, e) => OnMouseOver(item, e);
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
                        bar.y.Add((item, fill, x));
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                {
                    var bar = new Buffer<(T item, string fill, double value)>(8, 1);
                    bar.Add((item, fill, x));
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
                        bar.y.Add((item, fill, y));
                        exist = true;
                        break;
                    }
                }

                if (!exist)
                {
                    var bar = new Buffer<(T item, string fill, double value)>(8, 1);
                    bar.Add((item, fill, y));
                    bars.Add((x, bar));
                }
            }
        }

        private void Interactivity(Rectangle rect, T item, double x, double y)
        {
            if (OnClick != null)
            {
                rect.OnClick = e => OnClick(item, e);
            }

            if (onMouseOver != null)
            {
                rect.OnMouseOver = e => onMouseOver(item, x, y, e);
            }

            if (OnMouseOut != null)
            {
                rect.OnMouseOut = e => OnMouseOut(item, e);
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
                    var (x, y) = bars[i];
                    var sum = 0.0;

                    for (var j = y.Count - 1; j >= 0; j--)
                    {
                        var (item, fill, value) = y[j];

                        var rect = new Rectangle
                        {
                            Classes = animation ? "animate-bar" : string.Empty,
                            X = sum,
                            Y = x - delta / 2.0,
                            Width = value,
                            Height = delta,
                            Aesthetic = new Elements.Rectangle
                            {
                                Fill = fill,
                                Alpha = Aesthetic.Alpha
                            }
                        };

                        Interactivity(rect, item, sum + value, x);

                        Layer.Add(rect);

                        sum += value;
                    }

                    if (scale.x)
                    {
                        Positions.X.Position.Shape(0, sum);
                    }

                    if (scale.y)
                    {
                        Positions.Y.Position.Shape(x - delta, x + delta);
                    }
                }
            }
            else
            {
                for (var i = 0; i < bars.Count; i++)
                {
                    var (x, y) = bars[i];
                    var sum = 0.0;

                    for (var j = y.Count - 1; j >= 0; j--)
                    {
                        var (item, fill, value) = y[j];

                        var rect = new Rectangle
                        {
                            Classes = animation ? "animate-bar" : string.Empty,
                            X = x - delta / 2.0,
                            Y = sum,
                            Width = delta,
                            Height = value,
                            Aesthetic = new Elements.Rectangle
                            {
                                Fill = fill,
                                Alpha = Aesthetic.Alpha
                            }
                        };

                        Layer.Add(rect);

                        Interactivity(rect, item, x, sum + value);

                        sum += value;
                    }

                    if (scale.x)
                    {
                        Positions.X.Position.Shape(x - delta, x + delta);
                    }

                    if (scale.y)
                    {
                        Positions.Y.Position.Shape(0, sum);
                    }
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
                        var (item, fill, value) = bar.y[j];

                        var rect = new Rectangle
                        {
                            Classes = animation ? "animate-bar" : string.Empty,
                            X = x,
                            Y = value >= 0 ? 0 : value,
                            Width = w,
                            Height = Math.Abs(value),
                            Aesthetic = new Elements.Rectangle
                            {
                                Fill = fill,
                                Alpha = Aesthetic.Alpha
                            }
                        };

                        Interactivity(rect, item, x + w / 2.0, value);

                        Layer.Add(rect);

                        if (scale.x)
                        {
                            Positions.X.Position.Shape(x, x + w);
                        }

                        if (scale.y)
                        {
                            if (value >= 0)
                            {
                                Positions.Y.Position.Shape(0, value);
                            }
                            else
                            {
                                Positions.Y.Position.Shape(value, 0);
                            }
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

        public override void Clear()
        {
            base.Clear();

            bars.Clear();
        }
    }
}
