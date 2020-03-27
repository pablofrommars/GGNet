namespace GGNet
{
    using Elements;

    using static Units;

    public partial class Theme
    {
        public class _Tooltip
        {
            public _Tooltip()
            {
                Margin = new Margin(5, 10, 5, 10);

                Text = new Text
                {
                    Color = "#FFFFFF",
                    Size = new Size
                    {
                        Value = 0.75,
                        Units = em
                    }
                };

                Radius = new Size
                {
                    Value = 4,
                    Units = px
                };
            }

            public Margin Margin { get; set; }

            public Text Text { get; set; }

            public string Background { get; set; }

            public double? Alpha { get; set; }

            public Size Radius { get; set; }
        }
    }
}
