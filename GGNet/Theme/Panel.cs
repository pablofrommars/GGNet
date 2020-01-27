namespace GGNet
{
    using Elements;

    public class _Panel
    {
        public _Panel(bool dark)
        {
            var color = dark ? "#464950" : "#cccccc";

            Grid = new _Grid
            {
                Major = new _Grid._GridXY
                {
                    X = new Line
                    {
                        Width = 0.43,
                        Color = color
                    },
                    Y = new Line
                    {
                        Width = 0.43,
                        Color = color
                    }
                },
                Minor = new _Grid._GridXY
                {
                    X = new Line
                    {
                        Width = 0.32,
                        Color = color
                    },
                    Y = new Line
                    {
                        Width = 0.32,
                        Color = color
                    }
                },
            };

            Spacing = new _Spacing
            {
                X = 8,
                Y = 8
            };
        }

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
