using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class VStrip<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        public VStrip(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            Func<T, double> width,
            Buffer<Shape> layer = null)
            : base(source, null, false, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                Width = width,
            };
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, double> Width { get; set; }
        }

        public _Selectors Selectors { get; }

        public class _Positions
        {
            public IPositionMapping<T> X { get; set; }

            public IPositionMapping<T> Y { get; set; }
        }

        public _Positions Positions { get; } = new _Positions();

        public Func<T, MouseEventArgs, Task> OnClick { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOver { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOut { get; set; }

        public override void Init<T1, TX1, TY1>(Data<T1, TX1, TY1>.Panel panel, Facet<T1> facet)
        {
            base.Init(panel, facet);

            Positions.X = XMapping(Selectors.X, panel.X);
            Positions.Y = YMapping(Selectors.Y, panel.Y);
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
            Positions.Y.Train(item);
        }

        protected override void Shape(T item, bool flip)
        {
            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);
            var width = Selectors.Width(item);

            var strip = new VStrip
            {
                X = x,
                Y = y,
                Width = width
            };

            if (OnClick != null)
            {
                strip.OnClick = e => OnClick(item, e);
            }

            if (OnMouseOver != null)
            {
                strip.OnMouseOver = e => OnMouseOver(item, e);
            }

            if (OnMouseOut != null)
            {
                strip.OnMouseOut = e => OnMouseOut(item, e);
            }

            Layer.Add(strip);

            Positions.X.Position.Shape(x, x + width);
            Positions.Y.Position.Shape(y, y);
        }
    }
}
