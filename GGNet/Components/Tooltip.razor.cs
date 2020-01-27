using Microsoft.AspNetCore.Components;

using static System.Math;

namespace GGNet.Components
{
    public partial class Tooltip<T, TX, TY> : ComponentBase
        where TX : struct
        where TY : struct
    {
        [CascadingParameter]
        public Panel<T, TX, TY> Panel{ get; set; }

        protected bool visibility = false;

        protected string path;

        protected string[] content;
        protected (double x, double y, double width, double height) text;
        protected string anchor;
        protected string color;

        public void Show(double x, double y, string[] content, string color = null)
        {
            visibility = true;

            var _x = Panel.CoordX(x);
            var _y = Panel.CoordY(y);

            var width = 0.0;
            var height = 0.0;

            for (var i = 0; i < content.Length; i++)
            {
                width = Max(width, content[i].Width(Panel.Plot.Data.Theme.Tooltip.Size));
                height += content[i].Height(Panel.Plot.Data.Theme.Tooltip.Size);
            }

            width += Panel.Plot.Data.Theme.Tooltip.Margin.Left + Panel.Plot.Data.Theme.Tooltip.Margin.Right;
            height += Panel.Plot.Data.Theme.Tooltip.Margin.Top + Panel.Plot.Data.Theme.Tooltip.Margin.Bottom;

            if ((_x + width + 20.0) < (Panel.Area.X + Panel.Area.Width)
                && (_y + (height + 5) / 2) < (Panel.Y + Panel.Height)
                && (_y - (height + 5) / 2) > Panel.Y)
            {
                //right
                path = $"M{_x},{_y} l5 -2.5 v-{height / 2} q0,-2.5 5,-2.5 h{width} q5,0 5,2.5 v{height + 5} q0,2.5 -5,2.5 h-{width} q-5,0 -5,-2.5 v-{height / 2} l-5 -2.5 z";
                text = (_x + 7.5, _y - height / 2 - 2.5, width, height);
                anchor = "start";
            }
            else if ((_x - width - 20) > Panel.Area.X
                && (_y + (height + 5) / 2) < (Panel.Y + Panel.Height)
                && (_y - (height + 5) / 2) > Panel.Y)
            {
                //left
                path = $"M{_x},{_y} l-5 2.5 v{height / 2} q0,2.5 -5,2.5 h-{width} q-5,0 -5,-2.5 v-{height + 5} q0,-2.5 5,-2.5 h{width} q5,0 5,2.5 v{height / 2} l 5 2.5 z";
                text = (_x - 7.5, _y - height / 2 - 2.5, width, height);
                anchor = "end";
            }
            else if ((_x - (width + 20.0) / 2.0) > Panel.Area.X
                && (_x + (width + 5.0) / 2.0) < (Panel.Area.X + Panel.Area.Width)
                && (_y + height + 5) < (Panel.Y + Panel.Height))
            {
                //bottom
                path = $"M{_x},{_y} l2.5 5 h{width / 2} q5,0 5,2.5 v{height} q0,2.5 -5,2.5 h-{width + 5} q-5,0 -5,-2.5 v-{height} q0,-2.5 5,-2.5 h{width / 2.0} l2.5 -5 z";
                text = (_x - width / 2, _y + 5, width, height);
                anchor = "start";
            }
            else if ((_x - (width + 20.0) / 2.0) > Panel.Area.X
                && (_x + (width + 5.0) / 2.0) < (Panel.Area.X + Panel.Area.Width)
                && (_y - height - 5) > Panel.Y)
            {
                //top
                path = $"M{_x},{_y} l-2.5 -5 h-{width / 2} q-5,0 -5,-2.5 v-{height} q0,-2.5 5,-2.5 h{width + 5} q5,0 5,2.5 v{height} q0,2.5 -5,2.5 h-{width / 2} l-2.5 5 z";
                text = (_x - width / 2, _y - height - 10, width, height);
                anchor = "start";
            }
            else
            {
                return;
            }

            this.content = content;
            this.color = color;

            //StateHasChanged();
        }

        public void Hide()
        {
            visibility = false;

            //StateHasChanged();
        }
    }
}
