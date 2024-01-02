using GGNet.Common;
using GGNet.Geoms;

namespace GGNet.Data;

public sealed class Panel<T, TX, TY>((int row, int col) coord, PlotContext<T, TX, TY> context, double width, double height)
    where TX : struct
	where TY : struct
{
    public (int row, int col) Coord { get; } = coord;

    public PlotContext<T, TX, TY> Data { get; } = context;

    public double Width { get; } = width;

    public double Height { get; } = height;

    public string Id { get; } = $"{coord.row}_{coord.col}";

    public Buffer<IGeom> Geoms { get; } = new(8, 1);

	public (string? x, string? y) Strip { get; set; } = default;

	public (bool x, bool y) Axis { get; set; }

	public (double height, string? text) XLab { get; set; }

	public (double width, string? text) YLab { get; set; }

	public Components.IPanel? Component { get; set; }

	public void Register(Components.IPanel component)
	{
		Component = component;
	}

	internal Scales.Position<TX> X => Data.Positions.X.Scales.Count == 1
		? Data.Positions.X.Scales[0]
		: Data.Positions.X.Scales[Coord.row * Data.N.cols + Coord.col];

	internal Scales.Position<TY> Y => Data.Positions.Y.Scales.Count == 1
		? Data.Positions.Y.Scales[0]
		: Data.Positions.Y.Scales[Coord.row * Data.N.cols + Coord.col];
}