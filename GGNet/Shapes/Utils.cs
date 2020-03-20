using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;

namespace GGNet.Shapes
{
    public static class Extensions
    {
        public static async Task OnClickHandler(this Shape shape, MouseEventArgs e)
        {
            if (shape.OnClick != null)
            {
                await shape.OnClick(e);
            }
        }

        public static async Task OnMouseOverHandler(this Shape shape, MouseEventArgs e)
        {
            if (shape.OnMouseOver != null)
            {
                await shape.OnMouseOver(e);
            }
        }

        public static async Task OnMouseOutHandler(this Shape shape, MouseEventArgs e)
        {
            if (shape.OnMouseOut != null)
            {
                await shape.OnMouseOut(e);
            }
        }

        public static string Css(this Shape shape)
        {
            var ret = shape.Classes ?? "";

            if (shape.OnClick != null || shape.OnMouseOver != null || shape.OnMouseOut != null)
            {
                if (!string.IsNullOrEmpty(ret))
                {
                    ret += " ";
                }

                ret += "cursor-pointer";
            }

            return ret;
        }
    }
}
