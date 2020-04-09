using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Map<T> : Geom<T, double, double>
    {
        private readonly bool animation;

        public Map(
            Source<T> source,
            Func<T, Geospacial.Polygon[]> polygons,
            IAestheticMapping<T, string> fill = null,
            Func<T, (Geospacial.Point point, string content)> tooltip = null,
            bool animation = false,
            (bool x, bool y)? scale = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, scale, inherit, layer)
        {
            Selectors = new _Selectors
            {
                Polygons = polygons,
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
            public Func<T, Geospacial.Polygon[]> Polygons { get; set; }

            public Func<T, (Geospacial.Point point, string content)> Tooltip { get; set; }
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

        public Elements.Rectangle Aesthetic { get; set; }

        public override void Init<T1, TX1, TY1>(Data<T1, TX1, TY1>.Panel panel, Facet<T1> facet)
        {
            base.Init(panel, facet);

            Positions.X = XMapping(panel.Data.Selectors.X, panel.X);
            Positions.Y = YMapping(panel.Data.Selectors.Y, panel.Y);

            if (OnMouseOver == null && OnMouseOut == null && Selectors.Tooltip != null)
            {
                OnMouseOver = (item, _) =>
                {
                    var (point, content) = Selectors.Tooltip(item);

                    if (point != null)
                    {
                        panel.Component.Tooltip.Show(
                            point.Longitude,
                            point.Latitude, 
                            0,
                            content,
                            Aesthetics.Fill?.Map(item) ?? Aesthetic.Fill,
                            Aesthetic.Alpha);
                    }

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

            Aesthetics.Fill ??= panel.Data.Aesthetics.Fill as IAestheticMapping<T, string>;
        }

        public override void Train(T item)
        {
            Aesthetics.Fill?.Train(item);
        }

        public override void Legend()
        {
            Legend(Aesthetics.Fill, value => new Elements.Rectangle
            {
                Fill = value,
                Alpha = Aesthetic.Alpha,
                Color = Aesthetic.Color,
                Width = Aesthetic.Width
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

            var polygons = Selectors.Polygons(item);

            var xmin = double.MaxValue;
            var xmax = double.MinValue;

            var ymin = double.MaxValue;
            var ymax = double.MinValue;

            for (var i = 0; i < polygons.Length; i++)
            {
                var polygon = polygons[i];
                for (var j = 0; j < polygon.Longitude.Length; j++)
                {
                    var x = polygon.Longitude[j];
                    var y = polygon.Latitude[j];

                    if (x < xmin)
                    {
                        xmin = x;
                    }

                    if (x > xmax)
                    {
                        xmax = x;
                    }

                    if (y < ymin)
                    {
                        ymin = y;
                    }

                    if (y > ymax)
                    {
                        ymax = y;
                    }
                }
            }

            var multi = new MultiPolygon
            {
                Classes = animation ? "animate-map" : string.Empty,
                Polygons = polygons,
                Aesthetic = new Elements.Rectangle
                {
                    Fill = fill,
                    Alpha = Aesthetic.Alpha,
                    Color = Aesthetic.Color,
                    Width = Aesthetic.Width
                }
            };

            if (OnClick != null)
            {
                multi.OnClick = e => OnClick(item, e);
            }

            if (OnMouseOver != null)
            {
                multi.OnMouseOver = e => OnMouseOver(item, e);
            }

            if (OnMouseOut != null)
            {
                multi.OnMouseOut = e => OnMouseOut(item, e);
            }

            Layer.Add(multi);

            if (scale.x)
            {
                Positions.X.Position.Shape(xmin, xmax);
            }

            if (scale.y)
            {
                Positions.Y.Position.Shape(ymin, ymax);
            }
        }
    }
}
