using GGNet.Common;
using GGNet.Geoms;

namespace GGNet.Data;

public sealed class Panel<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public Panel((int row, int col) coord, Data<T, TX, TY> data, double width, double height)
	{
		Coord = coord;
		Data = data;
		Width = width;
		Height = height;

		Id = $"{coord.row}_{coord.col}";
	}

	public (int row, int col) Coord { get; }

	public Data<T, TX, TY> Data { get; }

	public double Width { get; }

	public double Height { get; }

	public string Id { get; }

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