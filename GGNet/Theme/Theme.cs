namespace GGNet
{
    using Elements;

    using static Position;
    using static Units;

    public partial class Theme
    {
        public Theme(bool dark, Position axisY, Position legend)
        {
            Plot = new _Plot(dark);

            Panel = new _Panel(dark);

            Axis = new _Axis(dark, axisY);

            Legend = new _Legend(dark, legend);

            Strip = new _Strip(dark);

            Tooltip = new Text
            {
                Size = new Size
                {
                    Value = 0.75,
                    Units = em
                },
                Color = "#FFF"
            };

            YLabel = new Text
            {
                Size = new Size
                {
                    Value = 0.75,
                    Units = em
                },
                Color = dark ? "#252A32" : "#FFFFFF",
                Fill = dark ? "#FFFFFF" : "#252A32"
            };
        }

        public _Plot Plot { get; set; }

        public _Panel Panel { get; set; }

        public _Axis Axis { get; set; }

        public _Legend Legend { get; set; }

        public _Strip Strip { get; set; }

        public Text Tooltip { get; set; }

        public Text YLabel { get; set; }

        public static Theme Default(bool dark = true, Position axisY = Left, Position legend = Right) => new Theme(dark, axisY, legend);
    }
}
