namespace GGNet.Shapes
{
    public class MultiPolygon : Shape
    {
        public Polygon[] Polygons { get; set; }

        public Elements.Rectangle Aesthetic { get; set; }
    }
}
