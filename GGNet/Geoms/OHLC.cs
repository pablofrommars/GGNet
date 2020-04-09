using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class OHLC<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private readonly ObjectPool<Line> linePool = new ObjectPool<Line>();

        public OHLC(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> open,
            Func<T, TY> high,
            Func<T, TY> low,
            Func<T, TY> close,
            (bool x, bool y)? scale = null,
            Buffer<Shape> layer = null)
            : base(source, scale, false, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Open = open,
                High = high,
                Low = low,
                Close = close
            };

            /*
            var aes = new Elements.Line
            {
                Width = width,
                Color = color,
                Alpha = alpha,
                LineType = LineType.Solid
            };

            mapping = new Mapping
            {
                Open = open,
                High = high,
                Low = low,
                Close = close
            };

            vstrip = new VStrip<T>(
                panel,
                x: o => panel.Data.Scales.X.Map(o) - 0.5,
                y: mapping.Close,
                width: o => 1.0,
                layer: Layer)
            {
                OnMouseOver = (item, _) =>
                {
                    if (vtrack)
                    {
                        Panel.Component.Plot.VTrack.Show(panel.Data.Scales.X.Map(item));
                    }

                    if (ylabel)
                    {
                        Panel.Component.YLabel.Show(mapping.Close(item));
                    }

                    return Task.CompletedTask;
                },
                OnMouseOut = (item, _) =>
                {
                    if (vtrack)
                    {
                        Panel.Component.Plot.VTrack.Hide();
                    }

                    if (ylabel)
                    {
                        Panel.Component.YLabel.Hide();
                    }

                    return Task.CompletedTask;
                }
            };

            this.open = new Segment<T>(
                panel,
                x: o => panel.Data.Scales.X.Map(o) - 0.5,
                xend: o => panel.Data.Scales.X.Map(o),
                y: mapping.Open,
                yend: mapping.Open,
                layer: Layer)
            {
                OnClick = onclick,
                OnMouseOver = OnMouseOver,
                OnMouseOut = OnMouseOut,
                Aesthetic = aes
            };

            range = new Segment<T>(
                panel,
                x: o => panel.Data.Scales.X.Map(o),
                xend: o => panel.Data.Scales.X.Map(o),
                y: mapping.High,
                yend: mapping.Low,
                layer: Layer)
            {
                OnClick = onclick,
                OnMouseOver = OnMouseOver,
                OnMouseOut = OnMouseOut,
                Aesthetic = aes
            };

            this.close = new Segment<T>(
                panel,
                x: o => panel.Data.Scales.X.Map(o),
                xend: o => panel.Data.Scales.X.Map(o) + 0.5,
                y: mapping.Close,
                yend: mapping.Close,
                layer: Layer)
            {
                OnClick = onclick,
                OnMouseOver = OnMouseOver,
                OnMouseOut = OnMouseOut,
                Aesthetic = aes
            };
            */
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Open { get; set; }

            public Func<T, TY> High { get; set; }

            public Func<T, TY> Low { get; set; }

            public Func<T, TY> Close { get; set; }
        }

        public _Selectors Selectors { get; }

        public class _Positions
        {
            public IPositionMapping<T> X { get; set; }

            public IPositionMapping<T> Open { get; set; }

            public IPositionMapping<T> High { get; set; }

            public IPositionMapping<T> Low { get; set; }

            public IPositionMapping<T> Close { get; set; }
        }

        public _Positions Positions { get; } = new _Positions();

        public Func<T, MouseEventArgs, Task> OnClick { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOver { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOut { get; set; }

        public Elements.Line Aesthetic { get; set; }

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

            Positions.Open = YMapping(Selectors.Open, panel.Y);
            Positions.High = YMapping(Selectors.High, panel.Y);
            Positions.Low = YMapping(Selectors.Low, panel.Y);
            Positions.Close = YMapping(Selectors.Close, panel.Y);
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
            Positions.Open.Train(item);
            Positions.High.Train(item);
            Positions.Low.Train(item);
            Positions.Close.Train(item);
        }

        protected override void Shape(T item, bool flip)
        {
            var x = Positions.X.Map(item);

            var open = Positions.Open.Map(item);
            var high = Positions.High.Map(item);
            var low = Positions.Low.Map(item);
            var close = Positions.Close.Map(item);

            Func<MouseEventArgs, Task> onmouseover = null;

            if (OnMouseOver != null)
            {
                onmouseover = e => OnMouseOver(item, e);
            }

            Func<MouseEventArgs, Task> onmouseout = null;

            if (OnMouseOut != null)
            {
                onmouseout = e => OnMouseOut(item, e);
            }

            var opening = linePool.Get();

            opening.X1 = x - 0.5;
            opening.X2 = x;
            opening.Y1 = open;
            opening.Y2 = open;
            opening.Aesthetic = Aesthetic;

            opening.OnMouseOver = onmouseover;
            opening.OnMouseOut = onmouseout;

            Layer.Add(opening);

            var range = linePool.Get();

            range.X1 = x;
            range.X2 = x;
            range.Y1 = low;
            range.Y2 = high;
            range.Aesthetic = Aesthetic;

            range.OnMouseOver = onmouseover;
            range.OnMouseOut = onmouseout;

            Layer.Add(range);

            var closing = linePool.Get();

            closing.X1 = x;
            closing.X2 = x + 0.5;
            closing.Y1 = close;
            closing.Y2 = close;
            closing.Aesthetic = Aesthetic;

            closing.OnMouseOver = onmouseover;
            closing.OnMouseOut = onmouseout;

            Layer.Add(closing);

            Positions.X.Position.Shape(x - 0.5, x + 0.5);
            /*
            Positions.Open.Position.Shape(open, open);
            Positions.High.Position.Shape(high, high);
            Positions.Low.Position.Shape(low, low);
            Positions.Close.Position.Shape(close, close);
            */
            Positions.Close.Position.Shape(low, high);
        }
        
        public override void Clear()
        {
            base.Clear();

            linePool.Reset();
        }
    }
}
