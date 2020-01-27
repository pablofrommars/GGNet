namespace GGNet
{
    using Elements;

    using static Anchor;
    using static Units;

    public partial class Theme
    {
        public class _Plot
        {
            public _Plot(bool dark)
            {
                Background = new Rectangle
                {
                    Fill = dark ? "#252A32" : "#FFFFFF"
                };

                Title = new Text
                {
                    Size = new Size
                    {
                        Value = 1.125,
                        Units = em
                    },
                    Anchor = start,
                    Weight = "bold",
                    Color = dark ? "#FFFFFF" : "#2b2b2b"
                };

                Title.Margin.Bottom = 8;

                SubTitle = new Text
                {
                    Size = new Size
                    {
                        Value = 0.8125,
                        Units = em
                    },
                    Anchor = start
                };

                if (!dark)
                {
                    SubTitle.Color = "#2b2b2b";
                }

                SubTitle.Margin.Bottom = 8;

                Caption = new Text
                {
                    Size = new Size
                    {
                        Value = 0.75,
                        Units = em
                    },
                    Anchor = end,
                    Style = "italic"
                };

                Caption.Margin.Top = 4;
                Caption.Margin.Right = 4;
                Caption.Margin.Bottom = 4;
                Caption.Margin.Left = 4;

                if (!dark)
                {
                    Caption.Color = "#2b2b2b";
                }
            }

            public Rectangle Background { get; set; }

            public Text Title { get; set; }

            public Text SubTitle { get; set; }

            public Text Caption { get; set; }
        }
    }
}
