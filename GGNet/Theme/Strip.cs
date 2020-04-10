namespace GGNet
{
    using Elements;

    using static Units;

    public class _Strip
    {
        public _Strip(bool dark)
        {
            Text = new _Text(dark);
        }

        public class _Text
        {
            public _Text(bool dark)
            {
                var color = dark ? "#929299" : "#212529";

                X = new Text()
                {
                    Size = new Size(0.75),
                    Color = color,
                    Weight = "bold"
                };

                X.Margin.Left = 4;
                X.Margin.Top = 4;
                X.Margin.Bottom = 4;

                Y = new Text()
                {
                    Size = new Size(0.75),
                    Color = color,
                    Weight = "bold",
                    Angle = 90
                };

                Y.Margin.Left = 4;
                Y.Margin.Top = 4;
                Y.Margin.Right = 4;
            }

            public Text X { get; set; }

            public Text Y { get; set; }
        }

        public _Text Text { get; set; }
    }
}
