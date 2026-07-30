using GGNet.Data;

namespace GGNet;

// Realization bucket: derived from spec + data, rebuilt or cleared every
// Render() pass — deterministic and disposable, never authored. One nuance:
// default-path Panels are built once and outlive passes (geoms capture
// container references at panel-build time); faceted panel sets are
// data-dependent and re-derived each pass.
public partial class PlotContext<T, TX, TY>
	where TX : struct
	where TY : struct
{
	// The plot-level strategy instance answers plot-level policy (axis bands,
	// expansion hints); panels materialize their own measured instances.
	// default!: assigned by Init, which Render runs first when needed.
	internal Coords.ICoordinateSystem Coord { get; private set; } = default!;

	internal List<Panel<T, TX, TY>> Panels { get; } = [];

	// default!: assigned by Init, which Render runs first when needed.
	internal Legends Legends { get; private set; } = default!;

	internal (int rows, int cols) N { get; private set; }

	internal double Strip { get; private set; }

	internal (double width, double height) Axis { get; private set; }

	internal (bool x, bool y) AxisVisibility { get; private set; }

	internal (double x, double y) AxisTitles { get; private set; }

	internal (bool x, bool y) AxisTitlesVisibility { get; private set; }
}
