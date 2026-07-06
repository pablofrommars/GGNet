using GGNet.Geoms;

namespace GGNet.Data;

public sealed class Panel<T, TX, TY>((int row, int col) coord, PlotContext<T, TX, TY> context, double width, double height, Func<MouseEventArgs, Task>? onClick)
  where TX : struct
  where TY : struct
{
	public (int row, int col) Coord { get; } = coord;

	public PlotContext<T, TX, TY> Data { get; } = context;

	public double Width { get; } = width;

	public double Height { get; } = height;

	public Func<MouseEventArgs, Task>? OnClick { get; } = onClick;

	public string Id { get; } = $"{coord.row}_{coord.col}";

	internal List<IGeom> Geoms { get; } = [];

	public (string? x, string? y) Strip { get; set; }

	public (bool x, bool y) Axis { get; set; }

	public (double height, string? text) XLab { get; set; }

	public (double width, string? text) YLab { get; set; }

	internal Components.IPanel? Component { get; set; }

	internal bool Registered { get; set; }

	internal void Register(Components.IPanel component)
	{
		Component = component;
		Registered = true;
	}

	internal Scales.Position<TX> X => Data.Positions.X.Scales.Count == 1
	  ? Data.Positions.X.Scales[0]
	  : Data.Positions.X.Scales[Coord.row * Data.N.cols + Coord.col];

	internal Scales.Position<TY> Y => Data.Positions.Y.Scales.Count == 1
	  ? Data.Positions.Y.Scales[0]
	  : Data.Positions.Y.Scales[Coord.row * Data.N.cols + Coord.col];
}
