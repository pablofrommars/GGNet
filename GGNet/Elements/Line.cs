namespace GGNet.Elements
{
    public class Line : IElement
    {
        public double Width { get; set; }

        public string Color { get; set; }

        public double Alpha { get; set; }

        public LineType LineType { get; set; }
    }

    public class VLine : Line { }

    public class HLine : Line { }
}
