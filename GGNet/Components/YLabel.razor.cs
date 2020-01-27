using Microsoft.AspNetCore.Components;

namespace GGNet.Components
{
    public partial class YLabel<T, TX, TY> : ComponentBase
        where TX : struct
        where TY : struct
    {
        [CascadingParameter]
        public Panel<T, TX, TY> Panel{ get; set; }

        protected bool visibility = false;
        protected (double x, double y, double width, double height) rect;
        protected (double x, double y, string text) label;

        public void Show(double y)
        {
            visibility = true;

            var _y = Panel.CoordY(y);
            
            var text = y.ToString("0.##");
            var width = text.Width(Panel.Plot.Data.Theme.YLabel.Size);
            var height = text.Height(Panel.Plot.Data.Theme.YLabel.Size);

            rect = (Panel.yAxisText.X, _y - 0.5 * height, width + 5, height + 5);
            label = (Panel.yAxisText.X + 2, _y + 0.5 * height + 2, text);

            StateHasChanged();
        }

        public void Hide()
        {
            visibility = false;

            StateHasChanged();
        }
    }
}
