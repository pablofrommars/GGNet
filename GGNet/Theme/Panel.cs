namespace GGNet
{
    using Elements;

    using static LineType;

    public class _Panel
    {
        public _Panel(bool dark)
        {
            Background = new Rectangle
            {
                Fill = dark ? "#343a40" : "#FFFFFF"
            };

            var color = dark ? "#464950" : "#cccccc";

            Grid = new _Grid
            {
                Major = new _Grid._GridXY
                {
                    X = new Line
                    {
                        Width = 0.43,
                        Fill = color,
                        Alpha = 1.0,
                        LineType = Solid
                    },
                    Y = new Line
                    {
                        Width = 0.43,
                        Fill = color,
                        Alpha = 1.0,
                        LineType = Solid
                    }
                },
                Minor = new _Grid._GridXY
                {
                    X = new Line
                    {
                        Width = 0.32,
                        Fill = color,
                        Alpha = 1.0,
                        LineType = Solid
                    },
                    Y = new Line
                    {
                        Width = 0.32,
                        Fill = color,
                        Alpha = 1.0,
                        LineType = Solid
                    }
                },
            };

            Spacing = new _Spacing
            {
                X = 8,
                Y = 8
            };
        }

        public Rectangle Background { get; set; }

        public class _Grid
        {
            public class _GridXY
            {
                public Line X { get; set; }

                public Line Y { get; set; }
            }

            public _GridXY Major { get; set; }

            public _GridXY Minor { get; set; }
        }

        public _Grid Grid { get; set; }

        public class _Spacing
        {
            public double X { get; set; }

            public double Y { get; set; }
        }

        public _Spacing Spacing { get; set; }
    }
}
