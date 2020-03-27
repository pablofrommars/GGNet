namespace GGNet.Components.Tooltips
{
    public partial class SparkLine: Tooltip
    {
        protected override string Render(double x, double y, double offset, string content)
        {
            var px = (x - Area.X) / Area.Width;

            var (role, tx) = (px < 0.5) switch
            {
                true => ("tooltip-right", x + offset),
                _ => ("tooltip-left", x - offset),
            };

            var ty = Area.Y + Area.Height / 2.0;

             return $@"
<foreignObject role=""{role}"" x=""{tx}"" y=""{ty}"" width=""1"" height=""1"">
        <div>
            <div class=""bubble"">{content}</div>
        </div>
    </foreignObject>
";
        }
    }
}
