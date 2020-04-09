using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Rectangle<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        public Rectangle(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            Func<T, double> width,
            Func<T, double> height,
            (bool x, bool y)? scale = null,
            Buffer<Shape> layer = null)
            : base(source, scale, false, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                Width = width,
                Height = height
            };
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, double> Width { get; set; }

            public Func<T, double> Height { get; set; }
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
        }

        public _Positions Positions { get; } = new _Positions();

        public Func<T, MouseEventArgs, Task> OnClick { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOver { get; set; }

        public Func<T, MouseEventArgs, Task> OnMouseOut { get; set; }

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
            var height = Selectors.Height(item);

            var rectangle = new Rectangle
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Aesthetic = Aesthetic
            };

            if (OnClick != null)
            {
                rectangle.OnClick = e => OnClick(item, e);
            }

            if (OnMouseOver != null)
            {
                rectangle.OnMouseOver = e => OnMouseOver(item, e);
            }

            if (OnMouseOut != null)
            {
                rectangle.OnMouseOut = e => OnMouseOut(item, e);
            }

            Layer.Add(rectangle);

            if (scale.x)
            {
                Positions.X.Position.Shape(x, x + width);
            }

            if (scale.y)
            {
                Positions.Y.Position.Shape(y, y + height);
            }
        }
    }
}
