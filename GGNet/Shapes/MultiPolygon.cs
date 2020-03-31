namespace GGNet.Shapes
{
    public class MultiPolygon : Shape
    {
        public Geospacial.Polygon[] Polygons { get; set; }

        public Elements.Rectangle Aesthetic { get; set; }
    }
}
