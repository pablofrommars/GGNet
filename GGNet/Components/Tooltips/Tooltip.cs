using Microsoft.AspNetCore.Components;

namespace GGNet.Components.Tooltips
{
    public abstract class Tooltip: ComponentBase, ITooltip
    {
        [Parameter]
        public string Id{ get; set; }

        [Parameter]
        public ICoord Coord{ get; set; }

        [Parameter]
        public Zone Area{ get; set; }

        [Parameter]
        public Theme Theme{ get; set; }

        protected bool visibility = false;

        protected string color;
        protected double alpha;
        protected string themeColor;
        protected double themeAlpha;

        protected string foreignObject;

        protected abstract string Render(double x, double y, double offset, string content);

        public void Show(double x, double y, double offset, string content, string color = null, double? alpha = null)
        {
            visibility = true;

            var _x = Coord.CoordX(x);
            var _y = Coord.CoordY(y);

            this.color = color ?? "#FFFFFF";
            this.alpha = alpha ?? 1.0;
            themeColor = Theme.Tooltip.Background ?? color ?? "#FFFFFF";
            themeAlpha = Theme.Tooltip.Alpha ?? alpha ?? 1.0;

            foreignObject = Render(_x, _y, offset, content);

            StateHasChanged();
        }

        public void Hide()
        {
            visibility = false;
            StateHasChanged();
        }
    }
}
