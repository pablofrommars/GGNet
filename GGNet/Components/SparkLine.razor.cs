using Microsoft.AspNetCore.Components;

using GGNet.Scales;

namespace GGNet.Components
{
    public partial class SparkLine<T, TX, TY> : ComponentBase, IPanel, ICoord
    where TX : struct
    where TY : struct
    {
        public SparkLine()
            : base()
        {
        }

        [Parameter]
        public Data<T, TX, TY> Data { get; set; }

        [Parameter]
        public double Width { get; set; } = 150;

        [Parameter]
        public double Height { get; set; } = 50;

        public string Id => Data.Id;

        public Theme Theme => Data.Theme;

        private Zone Area;
        private Data<T, TX, TY>.Panel Panel;
        private Position<TX> xscale;
        private Position<TY> yscale;

        protected Tooltips.SparkLine tooltip;
        public ITooltip Tooltip => tooltip;

        protected override void OnInitialized()
        {
            Area.Width = Width;
            Area.Height = Height;

            Data.Init(false);

            Data.Render(true);

            Panel = Data.Panels[0];
            xscale = Panel.X;
            yscale = Panel.Y;

            Panel.Register(this);

            Render();
        }

        public void Render()
        {
        }

        public double CoordX(double value) => Area.X + xscale.Coord(value) * Area.Width;

        public double CoordY(double value) => Area.Y + (1 - yscale.Coord(value)) * Area.Height;
    }
}
