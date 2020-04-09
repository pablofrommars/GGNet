using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

using static System.Math;

namespace GGNet.Geoms
{
    public class Segment<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        public Segment(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TX> xend,
            Func<T, TY> y,
            Func<T, TY> yend,
            (bool x, bool y)? scale = null,
            Buffer<Shape> layer = null)
            : base(source, scale, false, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                XEnd = xend,
                Y = y,
                YEnd = yend
            };
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TX> XEnd { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, TY> YEnd { get; set; }
        }

        public _Selectors Selectors { get; }

        public Func<T, MouseEventArgs, Task> OnClick { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOver { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOut { get; set; }

        public class _Positions
        {
            public IPositionMapping<T> X { get; set; }

            public IPositionMapping<T> XEnd { get; set; }

            public IPositionMapping<T> Y { get; set; }

            public IPositionMapping<T> YEnd { get; set; }
        }

        public _Positions Positions { get; } = new _Positions();

        public Elements.Line Aesthetic { get; set; }

        public override void Init<T1, TX1, TY1>(Data<T1, TX1, TY1>.Panel panel, Facet<T1> facet)
        {
            base.Init(panel, facet);

            Positions.X = XMapping(Selectors.X, panel.X);
            Positions.XEnd = XMapping(Selectors.XEnd, panel.X);
            Positions.Y = YMapping(Selectors.Y, panel.Y);
            Positions.YEnd = YMapping(Selectors.YEnd, panel.Y);
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
            Positions.XEnd.Train(item);
            Positions.Y.Train(item);
            Positions.YEnd.Train(item);
        }

        protected override void Shape(T item, bool flip)
        {
            var x = Positions.X.Map(item);
            var xend = Positions.XEnd.Map(item);
            var y = Positions.Y.Map(item);
            var yend = Positions.YEnd.Map(item);

            var line = new Line
            {
                X1 = x,
                X2 = xend,
                Y1 = y,
                Y2 = yend,
                Aesthetic = Aesthetic
            };

            if (OnClick != null)
            {
                line.OnClick = e => OnClick(item, e);
            }

            if (OnMouseOver != null)
            {
                line.OnMouseOver = e => OnMouseOver(item, e);
            }

            if (OnMouseOut != null)
            {
                line.OnMouseOut = e => OnMouseOut(item, e);
            }

            Layer.Add(line);

            if (scale.x)
            {
                Positions.X.Position.Shape(Min(x, xend), Max(x, xend));
            }

            if (scale.y)
            {
                Positions.Y.Position.Shape(Min(y, yend), Max(y, yend));
            }
        }
    }
}
