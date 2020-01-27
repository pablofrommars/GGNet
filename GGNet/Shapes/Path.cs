using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GGNet.Shapes
{
    public class Path : Shape
    {
        public Path() => Points = new SortedBuffer<(double x, double y)>(comparer: comparer);

        public SortedBuffer<(double x, double y)> Points { get; }

        private class Comparer : Comparer<(double x, double y)>
        {
            public override int Compare([AllowNull] (double x, double y) a, [AllowNull] (double x, double y) b) => a.x.CompareTo(b.x);
        }

        private static readonly Comparer comparer = new Comparer();

        public Elements.Line Aesthetic { get; set; }
    }
}
