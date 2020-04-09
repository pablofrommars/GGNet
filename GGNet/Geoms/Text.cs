using System;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class Text<T, TX, TY, TT> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        public Text(
            Source<T> source,
            Func<T, TX> x,
            Func<T, TY> y,
            Func<T, double> angle,
            Func<T, TT> text,
            IAestheticMapping<T, string> color = null,
            (bool x, bool y)? scale = null,
            bool inherit = true,
            Buffer<Shape> layer = null)
            : base(source, scale, inherit, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
                Y = y,
                Angle = angle,
                Text = text
            };

            Aesthetics = new _Aesthetics
            {
                Color = color
            };
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }

            public Func<T, TY> Y { get; set; }

            public Func<T, double> Angle { get; set; }

            public Func<T, TT> Text { get; set; }
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

            public IPositionMapping<T> XEnd { get; set; }

            public IPositionMapping<T> Y { get; set; }

            public IPositionMapping<T> YEnd { get; set; }
        }

        public _Positions Positions { get; } = new _Positions();

        public Elements.Text Aesthetic { get; set; }

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

            if (!inherit)
            {
                return;
            }

            Aesthetics.Color ??= panel.Data.Aesthetics.Color as IAestheticMapping<T, string>;
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
            Positions.Y.Train(item);
        }

        protected override void Shape(T item, bool flip)
        {
            var value = Selectors.Text(item)?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                return;
            }


            var clone = false;

            var color = Aesthetic.Color;
            if (Aesthetics.Color != null)
            {
                color = Aesthetics.Color.Map(item);
                if (string.IsNullOrEmpty(color))
                {
                    return;
                }

                clone = true;
            }

            var angle = Aesthetic.Angle;
            if (Selectors.Angle != null)
            {
                angle = Selectors.Angle(item);

                clone = true;
            }

            var x = Positions.X.Map(item);
            var y = Positions.Y.Map(item);

            var width = value.Width(Aesthetic.Size);
            var height = value.Height(Aesthetic.Size);

            var aes = Aesthetic;
            if (clone)
            {
                aes = aes.Clone();

                aes.Color = color;
                aes.Angle = angle;
            }

            var text = new Text
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Value = value,
                Aesthetic = aes
            };

            Layer.Add(text);

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
}
