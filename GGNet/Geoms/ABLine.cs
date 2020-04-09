using System;

using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class ABLine<T> : Geom<T, double, double>
    {
        private readonly (bool x, bool y) transformation;

        public ABLine(
            Source<T> source,
            Func<T, double> a,
            Func<T, double> b,
            Func<T, string> label,
            (bool x, bool y)? transformation = null,
            Buffer<Shape> layer = null)
            : base(source, null, false, layer)
        {
            this.transformation = transformation ?? (true, true);

            Selectors = new _Selectors
            {
                A = a,
                B = b,
                Label = label
            };
        }

        public class _Selectors
        {
            public Func<T, double> A { get; set; }

            public Func<T, double> B { get; set; }

            public Func<T, string> Label { get; set; }
        }

        public _Selectors Selectors { get; }

        public Elements.Line Line { get; set; }

        public Elements.Text Text { get; set; }

        public override void Train(T item) { }

        protected override void Shape(T item, bool flip)
        {
            var a = Selectors.A(item);
            var b = Selectors.B(item);

            string label = null;
            if (Selectors.Label != null)
            {
                label = Selectors.Label(item);
            }

            Layer.Add(new ABLine
            {
                A = a,
                B = b,
                Transformation = transformation,
                Label = label,
                Line = Line,
                Text = Text
            });
        }
    }
}
