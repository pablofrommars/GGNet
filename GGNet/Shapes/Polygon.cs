namespace GGNet.Shapes
{
    public class Polygon : Shape
    {
        public Polygon() => Points = new Buffer<(double x, double y)>(8, 1);

        public Buffer<(double x, double y)> Points { get; }

        public Elements.Rectangle Aesthetic { get; set; }
    }
}
