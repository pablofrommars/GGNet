namespace GGNet.Shapes
{
    public class Line : Shape
    {
        public double X1 { get; set; }

        public double X2 { get; set; }

        public double Y1 { get; set; }

        public double Y2 { get; set; }

        public Elements.Line Aesthetic { get; set; }
    }

    public class VLine : Shape
    {
        public double X { get; set; }

        public string Label { get; set; }

        public Elements.Line Line { get; set; }

        public Elements.Text Text { get; set; }
    }

    public class HLine : Shape
    {
        public double Y { get; set; }

        public string Label { get; set; }

        public Elements.Line Line { get; set; }

        public Elements.Text Text { get; set; }
    }

    public class ABLine : Shape
    {
        public double A { get; set; }

        public double B { get; set; }

        public (bool x, bool y) Transformation { get; set; }

        public string Label { get; set; }

        public Elements.Line Line { get; set; }

        public Elements.Text Text { get; set; }
    }
}
