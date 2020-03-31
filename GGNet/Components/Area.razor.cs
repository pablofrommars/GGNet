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

        private string Path(Shapes.Path path)
        {
            sb.Clear();

            var (x, y) = path.Points[0];

            sb.Append("M ");
            sb.Append(Coord.CoordX(x));
            sb.Append(" ");
            sb.Append(Coord.CoordY(y));

            for (var j = 1; j < path.Points.Count; j++)
            {
                (x, y) = path.Points[j];

                sb.Append(" L ");
                sb.Append(Coord.CoordX(x));
                sb.Append(" ");
                sb.Append(Coord.CoordY(y));
            }

            return sb.ToString();
        }

        private string Path(Shapes.Area area)
        {
            sb.Clear();

            var (x, ymin, ymax) = area.Points[0];

            sb.Append("M ");
            sb.Append(Coord.CoordX(x));
            sb.Append(" ");
            sb.Append(Coord.CoordY(ymax));

            for (var j = 1; j < area.Points.Count; j++)
            {
                (x, _, ymax) = area.Points[j];

                sb.Append(" L ");
                sb.Append(Coord.CoordX(x));
                sb.Append(" ");
                sb.Append(Coord.CoordY(ymax));
            }

            for (var j = 0; j < area.Points.Count; j++)
            {
                (x, ymin, _) = area.Points[area.Points.Count - j - 1];

                sb.Append(" L ");
                sb.Append(Coord.CoordX(x));
                sb.Append(" ");
                sb.Append(Coord.CoordY(ymin));
            }

            sb.Append(" Z");

            return sb.ToString();
        }

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

        private string Path(Geospacial.Polygon poly)
        {
            sb.Clear();

            AppendPolygon(poly);

            return sb.ToString();
        }

        private string Path(Geospacial.Polygon[] polygons)
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
