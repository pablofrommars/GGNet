namespace GGNet
{
    using Elements;

    using static Position;
    using static Direction;
    using static Anchor;
    using static Units;

    public partial class Theme
    {
        public class _Legend
        {
            public _Legend(bool dark, Position legend)
            {
                if (legend == Right)
                {
                    Position = Right;
                    Direction = Vertical;
                }
                else if (legend == Left)
                {
                    Position = Left;
                    Direction = Vertical;
                }
                else if (legend == Top)
                {
                    Position = Top;
                    Direction = Horizontal;
                }
                else
                {
                    Position = Bottom;
                    Direction = Horizontal;
                }

                var color = dark ? "#929299" : "#212529";

                Title = new Text
                {
                    Anchor = start,
                    Size = new Size(0.75),
                    Weight = "bold",
                    Color = color
                };

                Title.Margin.Top = 4;
                Title.Margin.Bottom = 4;

                Labels = new Text
                {
                    Anchor = start,
                    Size = new Size(0.75),
                    Color = color
                };

                Labels.Margin.Left = 4;

                Margin = new Margin();

                if (Direction == Horizontal)
                {
                    Title.Margin.Right = 16;
                    Labels.Margin.Right = 8;
                    Labels.Margin.Bottom = 4;
                }
                else
                {
                    Labels.Margin.Bottom = 4;

                    Margin.Left = 4;
                    Margin.Right = 4;
                }
            }

            public Position Position { get; set; }

            public Direction Direction { get; set; }

            public Text Title { get; set; }

            public Text Labels { get; set; }

            public Margin Margin { get; set; }
        }
    }
}
