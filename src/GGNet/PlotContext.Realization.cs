using GGNet.Data;

namespace GGNet;

public partial class PlotContext<T, TX, TY>
	where TX : struct
	where TY : struct
{
	internal Coords.CoordinateSystem Coord { get; private set; } = default!;

	internal List<Panel<T, TX, TY>> Panels { get; } = [];

	internal Legends Legends { get; private set; } = default!;

	internal (int rows, int cols) N { get; private set; }

	internal double Strip { get; private set; }

	internal (double width, double height) Axis { get; private set; }

	internal (bool x, bool y) AxisVisibility { get; private set; }

	internal (double x, double y) AxisTitles { get; private set; }

	internal (bool x, bool y) AxisTitlesVisibility { get; private set; }
}
