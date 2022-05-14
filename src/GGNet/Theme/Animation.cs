namespace GGNet.Theme;

public sealed class Animation
{
	public Animation()
	{
		Point = new();
		Bar = new();
		Map = new();
		Hex = new();
	}

	public PointAnimation Point { get; set; }

	public BarAnimation Bar { get; set; }

	public MapAnimation Map { get; set; }

	public HexAnimation Hex { get; set; }
}