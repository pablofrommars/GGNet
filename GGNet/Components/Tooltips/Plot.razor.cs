using Microsoft.AspNetCore.Components;

namespace GGNet.Components.Tooltips
{
    public partial class Plot: ComponentBase, ITooltip
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
            var py = 1.0 - ((_y - Area.Y) / Area.Height);

            var (role, tx, ty) = (px, py) switch
            {
                var (_px, _py) when _px < 0.2 && _py < 0.2 => ("tooltip-right-start", _x + offset, _y),
                var (_px, _py) when _px < 0.2 && _py > 0.8 => ("tooltip-right-end", _x + offset, _y),
                var (_px, _py) when _px > 0.8 && _py < 0.2 => ("tooltip-left-start", _x - offset, _y),
                var (_px, _py) when _px > 0.8 && _py > 0.8 => ("tooltip-left-end", _x - offset, _y),
                var (_px, _) when _px > 0.8 => ("tooltip-left-center", _x - offset, _y),
                var (_, _py) when _py > 0.8 => ("tooltip-bottom-center", _x, _y + offset),
                var (_, _py) when _py < 0.2 => ("tooltip-top-center", _x, _y - offset),
                _ => ("tooltip-right-center", _x + offset, _y)
            };

            foreignObject =
$@"
<foreignObject role=""{role}"" x=""{tx}"" y=""{ty}"" width=""1"" height=""1"">
        <div>
            <div class=""arrow""></div>
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
