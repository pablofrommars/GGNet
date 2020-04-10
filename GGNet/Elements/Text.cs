namespace GGNet.Elements
{
    using static Anchor;

    public class Text
    {
        public Text()
        {
            Size = new Size(1);

            Anchor = start;

            Weight = "normal";

            Style = "normal";

            Color = "#929299";

            Fill = "#FFFFFF";

            Alpha = 1.0;

            Angle = 0;

            Margin = new Margin();
        }

        public Size Size { get; set; }

        public Anchor Anchor { get; set; }

        public string Weight { get; set; }

        public string Style { get; set; }

        public string Color { get; set; }

        public string Fill { get; set; }

        public double Alpha { get; set; }

        public double Angle { get; set; }

        public Margin Margin { get; set; }

        public Text Clone() => new Text
        {
            Size = Size,
            Anchor = Anchor,
            Weight = Weight,
            Style = Style,
            Color = Color,
            Fill = Fill,
            Angle = Angle,
            Margin = Margin
        };
    }
}
