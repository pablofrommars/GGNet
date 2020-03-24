using Microsoft.AspNetCore.Components;

namespace GGNet.Components.Tooltips
{
    public partial class SparkLine: ComponentBase, ITooltip
    {
        [Parameter]
        public ICoord Coord{ get; set; }

        [Parameter]
        public Zone Area{ get; set; }

        [Parameter]
        public Theme Theme{ get; set; }

        protected bool visibility = false;

        protected string color;
        protected double alpha;
        protected string foreignObject;

        public void Show(double x, double y, double offset, string content, string color = null, double? alpha = null)
        {
            visibility = true;

            var _x = Coord.CoordX(x);
            var _y = Coord.CoordY(y);

            this.color = color ?? Theme.Tooltip.Fill;
            this.alpha = alpha ?? Theme.Tooltip.Alpha;

            var px = (_x - Area.X) / Area.Width;

            var (role, tx) = (px < 0.5) switch
            {
                true => ("tooltip-right", _x + offset),
                _ => ("tooltip-left", _x - offset),
            };

            var ty = Area.Y + Area.Height / 2.0;

            foreignObject =
$@"
<foreignObject role=""{role}"" x=""{tx}"" y=""{ty}"" width=""1"" height=""1"">
        <div>
            <div class=""bubble"">{content}</div>
        </div>
    </foreignObject>
";
            StateHasChanged();
        }

        public void Hide()
        {
            visibility = false;
            StateHasChanged();
        }
    }
}
