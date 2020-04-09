using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Volume<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private readonly ObjectPool<Rectangle> rectanglePool = new ObjectPool<Rectangle>();

        public Volume(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> volume,
            Buffer<Shape> layer = null)
            : base(source, null, false, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Volume = volume
            };

            /*
            vstrip = new VStrip<T>(
                panel,
                x: o => panel.Data.Scales.X.Map(o) - 0.5,
                y: mapping.Volume,
                width: o => 1.0,
                layer: Layer)
            {
                OnMouseOver = OnMouseOver,
                OnMouseOut = OnMouseOut
            };

            rectangle = new Rectangle<T>(
                panel,
                x: o => panel.Data.Scales.X.Map(o) - 0.45,
                y: _ => 0.0,
                width: _ => 0.9,
                height: mapping.Volume,
                layer: Layer)
            {
                OnClick = onclick,
                OnMouseOver = OnMouseOver,
                OnMouseOut = OnMouseOut,
                Aesthetic = new Elements.Rectangle
                { 
                    Fill = color,
                    Alpha = alpha
                }
            };
            */
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Volume { get; set; }
        }

        public _Selectors Selectors { get; }

        public Func<T, MouseEventArgs, Task> OnClick { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOver { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOut { get; set; }

        public class _Positions
        {
            public IPositionMapping<T> X { get; set; }

            public IPositionMapping<T> Volume { get; set; }
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

            Positions.Volume = YMapping(Selectors.Volume, panel.Y);
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
            Positions.Volume.Train(item);
        }

        protected override void Shape(T item, bool flip)
        {
            var x = Positions.X.Map(item);
            var volume = Positions.Volume.Map(item);

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

            var rectangle = rectanglePool.Get();

            rectangle.X = x - 0.45;
            rectangle.Y = 0;
            rectangle.Width = 0.9;
            rectangle.Height = volume;
            rectangle.Aesthetic = Aesthetic;

            rectangle.OnMouseOver = onmouseover;
            rectangle.OnMouseOut = onmouseout;

            Layer.Add(rectangle);

            Positions.X.Position.Shape(x - 0.45, x + 0.45);
            Positions.Volume.Position.Shape(0, volume);
        }

        public override void Clear()
        {
            base.Clear();

            rectanglePool.Reset();
        }
    }
}
