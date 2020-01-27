using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace GGNet.Shapes
{
    public class Area : Shape
    {
        public Area() => Points = new SortedBuffer<(double x, double ymin, double ymax)>(comparer: comparer);

        public SortedBuffer<(double x, double ymin, double ymax)> Points { get; }

        private class Comparer : Comparer<(double x, double ymin, double ymax)>
        {
            public override int Compare([AllowNull] (double x, double ymin, double ymax) a, [AllowNull] (double x, double ymin, double ymax) b) => a.x.CompareTo(b.x);
        }

        private static readonly Comparer comparer = new Comparer();

        public Elements.Rectangle Aesthetic { get; set; }
    }
}
