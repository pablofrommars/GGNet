using System;

using GGNet.Scales;
using GGNet.Facets;
using GGNet.Shapes;

namespace GGNet.Geoms
{
    public class VLine<T, TX, TY> : Geom<T, TX, TY>
        where TX : struct
        where TY : struct
    {
        public VLine(
            Source<T> source,
            Func<T, TX> x,
            Buffer<Shape> layer = null)
            : base(source, false, layer)
        {
            Selectors = new _Selectors
            {
                X = x,
            };
        }

        public class _Selectors
        {
            public Func<T, TX> X { get; set; }
        }

        public _Selectors Selectors { get; }

        public class _Positions
        {
            public IPositionMapping<T> X { get; set; }
        }

        public _Positions Positions { get; } = new _Positions();

        public Elements.Line Aesthetic { get; set; }

        public override void Init<T1, TX1, TY1>(Data<T1, TX1, TY1>.Panel panel, Facet<T1> facet)
        {
            base.Init(panel, facet);

            Positions.X = XMapping(Selectors.X, panel.X);
        }

        public override void Train(T item)
        {
            Positions.X.Train(item);
        }

        protected override void Shape(T item, bool flip)
        {
            var x = Positions.X.Map(item);

            Layer.Add(new VLine
            {
                X = x,
                Aesthetic = Aesthetic
            });

            //Positions.X.Position.Shape(x, x);
        }
    }
}
