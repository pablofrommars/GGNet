namespace GGNet.Components.Tooltips;

public partial class Plot : Tooltip
{
	protected override string Render(double x, double y, double offset, string content)
	{
		var px = (x - Area.X) / Area.Width;
		var py = 1.0 - ((y - Area.Y) / Area.Height);

		var (role, tx, ty) = (px, py) switch
		{
			var (_px, _py) when _px < 0.25 && _py < 0.25 => ("tooltip-right-start", x + offset, y),
			var (_px, _py) when _px < 0.25 && _py > 0.75 => ("tooltip-right-end", x + offset, y),
			var (_px, _py) when _px > 0.75 && _py < 0.25 => ("tooltip-left-start", x - offset, y),
			var (_px, _py) when _px > 0.8 && _py > 0.75 => ("tooltip-left-end", x - offset, y),
			var (_px, _) when _px > 0.75 => ("tooltip-left-center", x - offset, y),
			var (_, _py) when _py > 0.75 => ("tooltip-bottom-center", x, y + offset),
			var (_, _py) when _py < 0.25 => ("tooltip-top-center", x, y - offset),
			_ => ("tooltip-right-center", x + offset, y)
		};

		return $@"
<foreignObject role=""{role}"" x=""{tx}"" y=""{ty}"" width=""1"" height=""1"">
        <div>
            <div class=""arrow""></div>
            <div class=""bubble"">{content}</div>
        </div>
    </foreignObject>
";
	}
}