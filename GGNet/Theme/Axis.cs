namespace GGNet
{
    using Elements;

    using static Position;
    using static Anchor;
    using static Units;

    public partial class Theme
    {
        public class _Axis
        {
            public _Axis(bool dark, Position axisY)
            {
                axisY = axisY == Right ? Right : Left;

                Y = axisY;
                Text = new _Text(dark, axisY);
                Title = new _Title(dark, axisY);
            }

            public Position Y { get; set; }

            public class _Text
            {
                public _Text(bool dark, Position axisY)
                {
                    X = new Text
                    {
                        Size = new Size(0.75),
                        Anchor = middle
                    };

                    Y = new Text
                    {
                        Size = new Size(0.75),
                        Anchor = axisY == Left ? end : start
                    };

                    Y.Margin.Left = 4;
                    Y.Margin.Right = 4;

                    if (!dark)
                    {
                        X.Color = "#2b2b2b";
                        Y.Color = "#2b2b2b";
                    }
                }

                public Text X { get; set; }

                public Text Y { get; set; }
            }

            public _Text Text { get; set; }

            public class _Title
            {
                public _Title(bool dark, Position axisY)
                {
                    X = new Text
                    { 
                        Anchor = end,
                        Size = new Size(0.75)
                    };

                    X.Margin.Top = 4;
                    X.Margin.Right = 4;
                    X.Margin.Bottom = 4;

                    if (axisY == Left)
                    {
                        Y = new Text
                        {
                            Angle = -90,
                            Anchor = end,
                            Size = new Size(0.75)
                        };

                        Y.Margin.Right = 4;
                    }
                    else
                    {
                        Y = new Text
                        {
                            Angle = 90,
                            Anchor = start,
                            Size = new Size(0.75)
                        };

                        Y.Margin.Top = 4;
                        Y.Margin.Right = 4;
                        Y.Margin.Left = 4;
                    }

                    if (!dark)
                    {
                        X.Color = "#2b2b2b";
                        Y.Color = "#2b2b2b";
                    }
                }

                public Text X { get; set; }

                public Text Y { get; set; }
            }

            public _Title Title { get; set; }
        }
    }
}
