using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Point<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        public Point(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            IAestheticMapping<T, double> size,
            IAestheticMapping<T, string> color,
            Func<T, string[]> tooltip = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, inherit, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                Tooltip = tooltip
            };

            Aesthetics = new _Aesthetics
            {
                Size = size,
                Color = color,
            };
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, string[]> Tooltip { get; set; }
        }

        public _Selectors Selectors { get; }

        public class _Aesthetics
        {
            public IAestheticMapping<T, double> Size { get; set; }

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

        public Elements.Circle Aesthetic { get; set; }

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
                OnMouseOver = (item, _) =>
                {
                    panel.Component.Tooltip.Show(
                        Positions.X.Map(item),
                        Positions.Y.Map(item),
                        Selectors.Tooltip(item),
                        Aesthetics.Color?.Map(item) ?? Aesthetic.Fill); 

                    return Task.CompletedTask;
                };

                OnMouseOut = (_, __) =>
                {
                    panel.Component.Tooltip.Hide();

                    return Task.CompletedTask;
                };
            }

            if (!inherit)
            {
                return;
            }

            Aesthetics.Size ??= panel.Data.Aesthetics.Size as IAestheticMapping<T, double>;
            Aesthetics.Color ??= panel.Data.Aesthetics.Color as IAestheticMapping<T, string>;
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
            Positions.Y.Train(item);

            Aesthetics.Size?.Train(item);
            Aesthetics.Color?.Train(item);
        }

        public override void Legend()
        {
            Legend(Aesthetics.Color, value => new Elements.Circle
            {
                Fill = value,
                Alpha = Aesthetic.Alpha,
                Radius = Aesthetic.Radius
            });

            Legend(Aesthetics.Size, value => new Elements.Circle
            {
                Fill = Aesthetic.Fill,
                Alpha = Aesthetic.Alpha,
                Radius = value
            });
        }

        protected override void Shape(T item, bool flip)
        {
            var color = Aesthetic.Fill;

            if (Aesthetics.Color != null)
            {
                color = Aesthetics.Color.Map(item);
                if (string.IsNullOrEmpty(color))
                {
                    return;
                }
            }

            var radius = Aesthetic.Radius;

            if (Aesthetics.Size != null)
            {
                radius = Aesthetics.Size.Map(item);
                if (radius <= 0)
                {
                    return;
                }
            }

            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);

            var circle = new Circle
            {
                X = x,
                Y = y,
                Aesthetic = new Elements.Circle
                {
                    Radius = radius,
                    Fill = color,
                    Alpha = Aesthetic.Alpha
                },
            };

            if (OnClick != null)
            {
                circle.OnClick = e => OnClick(item, e);
            }

            if (OnMouseOver != null)
            {
                circle.OnMouseOver = e => OnMouseOver(item, e);
            }

            if (OnMouseOut != null)
            {
                circle.OnMouseOut = e => OnMouseOut(item, e);
            }

            Layer.Add(circle);

            Positions.X.Position.Shape(x, x);
            Positions.Y.Position.Shape(y, y);
        }
    }
}
