using System;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Candlestick<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private readonly ObjectPool<Line> linePool = new ObjectPool<Line>();
        private readonly ObjectPool<Rectangle> rectanglePool = new ObjectPool<Rectangle>();

        public Candlestick(
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

        public Elements.Line Line { get; set; }
        public Elements.Rectangle Rectangle { get; set; }

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

            if (close >= open)
            {
                var a = linePool.Get();
                a.X1 = x - 0.45;
                a.X2 = x + 0.45;
                a.Y1 = close;
                a.Y2 = close;
                a.Aesthetic = Line;
                Layer.Add(a);

                var b = linePool.Get();
                b.X1 = x + 0.45;
                b.X2 = x + 0.45;
                b.Y1 = close;
                b.Y2 = open;
                b.Aesthetic = Line;
                Layer.Add(b);

                var c = linePool.Get();
                c.X1 = x + 0.45;
                c.X2 = x - 0.45;
                c.Y1 = open;
                c.Y2 = open;
                c.Aesthetic = Line;
                Layer.Add(c);

                var d = linePool.Get();
                d.X1 = x - 0.45;
                d.X2 = x - 0.45;
                d.Y1 = open;
                d.Y2 = close;
                d.Aesthetic = Line;
                Layer.Add(d);

                var e = linePool.Get();
                e.X1 = x;
                e.X2 = x;
                e.Y1 = close;
                e.Y2 = high;
                e.Aesthetic = Line;
                Layer.Add(e);

                var f = linePool.Get();
                f.X1 = x;
                f.X2 = x;
                f.Y1 = open;
                f.Y2 = low;
                f.Aesthetic = Line;
                Layer.Add(f);
            }
            else
            {
                var e = linePool.Get();
                e.X1 = x;
                e.X2 = x;
                e.Y1 = open;
                e.Y2 = high;
                e.Aesthetic = Line;
                Layer.Add(e);

                var f = linePool.Get();
                f.X1 = x;
                f.X2 = x;
                f.Y1 = close;
                f.Y2 = low;
                f.Aesthetic = Line;
                Layer.Add(f);

                var r = rectanglePool.Get();
                r.X = x - 0.45;
                r.Y = close;
                r.Width = 0.9;
                r.Height = open - close;
                r.Aesthetic = Rectangle;
                Layer.Add(r);
            }

            Positions.X.Position.Shape(x - 0.45, x + 0.45);
            Positions.Close.Position.Shape(low, high);
        }

        public override void Clear()
        {
            base.Clear();

            linePool.Reset();
            rectanglePool.Reset();
        }
    }
}
