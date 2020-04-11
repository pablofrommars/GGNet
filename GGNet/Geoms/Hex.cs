using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Hex<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        private readonly bool animation;

        public Hex(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            Func<T, TX> Dx,
            Func<T, TY> Dy,
            IAestheticMapping<T, string> fill = null,
            Func<T, string> tooltip = null,
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
                Dx = Dx,
                Dy = Dy,
                Tooltip = tooltip
            };

            Aesthetics = new _Aesthetics
            {
                Fill = fill
            };

            this.animation = animation;
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, TX> Dx { get; set; }

            public Func<T, TY> Dy { get; set; }

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

            public IPositionMapping<T> Dx { get; set; }

            public IPositionMapping<T> Dy { get; set; }
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

            Positions.Dx = XMapping(Selectors.Dx, panel.X);
            Positions.Dy = YMapping(Selectors.Dy, panel.Y);

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
            Positions.Dx.Train(item);
            Positions.Dy.Train(item);

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
            var dx = Positions.Dx.Map(item) / 2.0;
            var dy = Positions.Dy.Map(item) / Math.Sqrt(3.0) / 2.0 * 1.15;

            var hex = new Polygon
            {
                Classes = animation ? "animate-hex" : string.Empty,
                Path = new Geospacial.Polygon
                {
                    Longitude = new[] { x + dx, x + dx, x, x - dx, x - dx, x },
                    Latitude = new[] { y + dy, y - dy, y - 2.0 * dy, y - dy, y + dy, y + 2.0 * dy }
                },
                Aesthetic = new Elements.Rectangle
                {
                    Fill = fill,
                    Alpha = Aesthetic.Alpha
                }
            };

            if (OnClick != null)
            {
                hex.OnClick = e => OnClick(item, e);
            }

            if (onMouseOver != null)
            {
                hex.OnMouseOver = e => onMouseOver(item, x, y, e);
            }

            if (OnMouseOut != null)
            {
                hex.OnMouseOut = e => OnMouseOut(item, e);
            }

            Layer.Add(hex);

            if (scale.x)
            {
                Positions.X.Position.Shape(x - dx, x + dx);
            }

            if (scale.y)
            {
                Positions.Y.Position.Shape(y - 2.0 * dy, y + 2.0 * dy);
            }
        }
    }
}
