namespace GGNet
{
    using static Position;

    public partial class Theme
    {
        public Theme(bool dark, Position axisY, Position legend)
        {
            FontFamily = "-apple-system,BlinkMacSystemFont,\"Segoe UI\",Roboto,\"Helvetica Neue\",Arial,\"Noto Sans\",sans-serif,\"Apple Color Emoji\",\"Segoe UI Emoji\",\"Segoe UI Symbol\",\"Noto Color Emoji\"";

            Plot = new _Plot(dark);

            Panel = new _Panel(dark);

            Axis = new _Axis(dark, axisY);

            Legend = new _Legend(dark, legend);

            Strip = new _Strip(dark);

            Animation = new _Animation();

            Tooltip = new _Tooltip();
        }

        public string FontFamily { get; set; }

        public _Plot Plot { get; set; }

        public _Panel Panel { get; set; }

        public _Axis Axis { get; set; }

        public _Legend Legend { get; set; }

        public _Strip Strip { get; set; }

        public _Animation Animation { get; set; }

        public _Tooltip Tooltip { get; set; }

        public static Theme Default(bool dark = true, Position axisY = Left, Position legend = Right) => new Theme(dark, axisY, legend);
    }
}
