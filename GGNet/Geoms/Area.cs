using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Area<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private class Comparer : Comparer<(double x, double y, T item)>
        {
            public override int Compare((double x, double y, T item) a, (double x, double y, T item) b) => a.x.CompareTo(b.x);
        }

        private static readonly Comparer comparer = new Comparer();

        private readonly Buffer<(string fill, SortedBuffer<(double x, double y, T item)> points)> series = new Buffer<(string fill, SortedBuffer<(double x, double y, T item)> points)>(16, 1);

        private readonly PositionAdjustment position;

        public Area(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            IAestheticMapping<T, string> fill = null,
            Func<T, string> tooltip = null,
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
                Tooltip = tooltip
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

            SortedBuffer<(double x, double y, T item)> points = null;

            for (var i = 0; i < series.Count; i++)
            {
                var serie = series[i];
                if (serie.fill == fill)
                {
                    points = serie.points;
                    break;
                }
            }

            if (points == null)
            {
                points = new SortedBuffer<(double x, double y, T item)>(comparer: comparer);

                series.Add((fill, points));
            }

            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);

            points.Add((x, y, item));
        }

        private void Interactivity(Buffer<Shape> circles, T item, double x, double y)
        {
            if (OnClick != null || onMouseOver != null || OnMouseOut != null)
            {
                var circle = new Circle
                {
                    X = x,
                    Y = y,
                    Aesthetic = new Elements.Circle
                    {
                        Radius = 3.0,
                        Alpha = 0,
                        Fill = "transparent"
                    }
                };

                if (OnClick != null)
                {
                    circle.OnClick = e => OnClick(item, e);
                }

                if (onMouseOver != null)
                {
                    circle.OnMouseOver = e => onMouseOver(item, x, y, e);
                }

                if (OnMouseOut != null)
                {
                    circle.OnMouseOut = e => OnMouseOut(item, e);
                }

                circles.Add(circle);
            }
        }

        private class SumComparer : Comparer<(double x, double y)>
        {
            public override int Compare((double x, double y) a, (double x, double y) b) => a.x.CompareTo(b.x);
        }

        private static readonly SumComparer sumComparer = new SumComparer();

        private void Stack(bool flip)
        {
            var sum = new SortedBuffer<(double x, double y)>(comparer: sumComparer);
            var circles = new Buffer<Shape>();

            for (var i = 0; i < series.Count; i++)
            {
                var (_, points) = series[i];
                for (var j = 0; j < points.Count; j++)
                {
                    var (x, _, _) = points[j];

                    sum.Add((x, 0));
                }
            }

            for (var i = 0; i < series.Count; i++)
            {
                var (fill, points) = series[i];

                var area = new Area
                {
                    Aesthetic = new Elements.Rectangle
                    {
                        Fill = fill,
                        Alpha = Aesthetic.Alpha
                    }
                };

                Layer.Add(area);

                var ylast = 0.0;
                var xlast = 0.0;
                var slast = 0.0;

                var head = 0;

                {
                    var (x, y, item) = points[0];

                    while (head < sum.Count)
                    {
                        var (xhead, yhead) = sum[head];

                        if (xhead == x)
                        {
                            ylast = y;
                            slast = yhead + y;

                            area.Points.Add((x, yhead, slast));

                            if (scale.y)
                            {
                                Positions.Y.Position.Shape(0, slast);
                            }

                            sum[head++] = (x, slast);
                            xlast = x;

                            break;
                        }

                        head++;
                    }

                    Interactivity(circles, item, x, slast);

                    if (scale.x)
                    {
                        Positions.X.Position.Shape(x, x);
                    }
                }

                for (var j = 1; j < points.Count; j++)
                {
                    var k = head;
                    var (x, y, item) = points[j];

                    var (xk, _) = sum[k];

                    var xhead = xk;

                    while (head < sum.Count)
                    {
                        (xhead, _) = sum[head];

                        if (xhead == x)
                        {
                            break;
                        }

                        head++;
                    }

                    if (head == k)
                    {
                        var (sx, sy) = sum[head];
                        slast = sy + y;
                        area.Points.Add((x, sy, slast));
                        sum[k] = (x, slast);
                    }
                    else
                    {
                        var xdelta = xhead - xlast;
                        var ydelta = y - ylast;

                        if (ydelta >= 0)
                        {
                            for (; k <= head; k++)
                            {
                                var (sx, sy) = sum[k];
                                var delta = sy + (sx - xlast) / xdelta * ydelta;
                                area.Points.Add((sx, sy, delta));
                                sum[k] = (sx, delta);
                                slast = delta;
                            }
                        }
                        else
                        {
                            for (; k <= head; k++)
                            {
                                var (sx, sy) = sum[k];
                                var delta = sy + ylast + (sx - xlast) / xdelta * ydelta;
                                area.Points.Add((sx, sy, delta));
                                sum[k] = (sx, delta);
                                slast = delta;
                            }
                        }
                    }

                    Interactivity(circles, item, x, slast);

                    if (scale.y)
                    {
                        Positions.Y.Position.Shape(0, slast);
                    }

                    xlast = x;
                    ylast = y;

                    if (scale.x)
                    {
                        Positions.X.Position.Shape(x, x);
                    }

                    head++;
                }
            }

            Layer.Add(circles);
        }

        private void Identity(bool flip)
        {
            var circles = new Buffer<Shape>();

            for (var i = 0; i < series.Count; i++)
            {
                var (fill, points) = series[i];

                var area = new Area
                {
                    Aesthetic = new Elements.Rectangle
                    {
                        Fill = fill,
                        Alpha = Aesthetic.Alpha
                    }
                };

                Layer.Add(area);

                for (var j = 0; j < points.Count; j++)
                {
                    var (x, y, item) = points[j];

                    area.Points.Add((x, 0, y));

                    Interactivity(circles, item, x, y);

                    if (scale.x)
                    {
                        Positions.X.Position.Shape(x, x);
                    }

                    if (scale.y)
                    {
                        Positions.Y.Position.Shape(y, y);
                    }
                }
            }

            Layer.Add(circles);
        }

        protected override void Set(bool flip)
        {
            if (position == PositionAdjustment.Stack)
            {
                Stack(flip);
            }
            else if (position == PositionAdjustment.Identity)
            {
                Identity(flip);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void Clear()
        {
            base.Clear();

            series.Clear();
        }
    }
}
