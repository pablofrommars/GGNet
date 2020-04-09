using System;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class HLine<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        public HLine(
            Source<T> source,
            Func<T, TY> y,
            Func<T, string> label,
            Buffer<Shape> layer = null)
            : base(source, null, false, layer)
        {
            Selectors = new _Selectors
            {
                Y = y,
                Label = label
            };
        }

        public class _Selectors
        {
            public Func<T, TY> Y { get; set; }

            public Func<T, string> Label { get; set; }
        }

        public _Selectors Selectors { get; }

        public class _Positions
        {
            public IPositionMapping<T> Y { get; set; }
        }

        public _Positions Positions { get; } = new _Positions();

        public Elements.Line Line { get; set; }

        public Elements.Text Text { get; set; }

        public override void Init<T1, TX1, TY1>(Data<T1, TX1, TY1>.Panel panel, Facet<T1> facet)
        {
            base.Init(panel, facet);

            Positions.Y = YMapping(Selectors.Y, panel.Y);
        }

        public override void Train(T item)
        {
            Positions.Y.Train(item);
        }

        protected override void Shape(T item, bool flip)
        {
            var y = Positions.Y.Map(item);

            string label = null;
            if (Selectors.Label != null)
            {
                label = Selectors.Label(item);
            }

            Layer.Add(new HLine
            {
                Y = y,
                Label = label,
                Line = Line,
                Text = Text
            });
        }
    }
}
