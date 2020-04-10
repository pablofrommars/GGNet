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
                    Fill = dark ? "#343a40" : "#FFFFFF"
                };

                Title = new Text
                {
                    Size = new Size(1.125),
                    Anchor = start,
                    Weight = "bold",
                    Color = dark ? "#FFFFFF" : "#212529"
                };

                Title.Margin.Bottom = 8;

                SubTitle = new Text
                {
                    Size = new Size(0.8125),
                    Anchor = start
                };

                if (!dark)
                {
                    SubTitle.Color = "#212529";
                }

                SubTitle.Margin.Bottom = 8;

                Caption = new Text
                {
                    Size = new Size(0.75),
                    Anchor = end,
                    Style = "italic"
                };

                Caption.Margin.Top = 4;
                Caption.Margin.Right = 4;
                Caption.Margin.Bottom = 4;
                Caption.Margin.Left = 4;

                if (!dark)
                {
                    Caption.Color = "#212529";
                }
            }

            public Rectangle Background { get; set; }

            public Text Title { get; set; }

            public Text SubTitle { get; set; }

            public Text Caption { get; set; }
        }
    }
}
