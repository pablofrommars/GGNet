using System.Text;

using Microsoft.AspNetCore.Components;

namespace GGNet.Components
{
    public partial class Area<T, TX, TY> : ComponentBase
       where TX : struct
       where TY : struct
    {
        [Parameter]
        public Data<T, TX, TY>.Panel Data { get; set; }

        [Parameter]
        public RenderChildPolicyBase RenderPolicy { get; set; }

        [Parameter]
        public ICoord Coord { get; set; }

        [Parameter]
        public Zone Zone { get; set; }

        [Parameter]
        public string Clip { get; set; }

        private StringBuilder sb = new StringBuilder();

        protected override bool ShouldRender() => RenderPolicy.ShouldRender();

        private void AppendPolygon(Geospacial.Polygon poly)
        {
            sb.Append("M ");
            sb.Append(Coord.CoordX(poly.Longitude[0]));
            sb.Append(" ");
            sb.Append(Coord.CoordY(poly.Latitude[0]));

            for (var i = 1; i < poly.Longitude.Length; i++)
            {
                sb.Append(" L ");
                sb.Append(Coord.CoordX(poly.Longitude[i]));
                sb.Append(" ");
                sb.Append(Coord.CoordY(poly.Latitude[i]));
            }

            sb.Append(" Z");
        }

        private string PolygonPath(Geospacial.Polygon poly)
        {
            sb.Clear();

            AppendPolygon(poly);

            return sb.ToString();
        }

        private string PolygonPath(Geospacial.Polygon[] polygons)
        {
            sb.Clear();

            AppendPolygon(polygons[0]);

            for (var i = 1; i < polygons.Length; i++)
            {
                sb.Append(" ");

                AppendPolygon(polygons[i]);
            }

            return sb.ToString();
        }
    }
}
