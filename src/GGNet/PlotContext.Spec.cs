using GGNet.Data;
using GGNet.Facets;

namespace GGNet;

// Spec bucket: what the fluent API authors during build — written by the
// builder extensions, frozen by convention (not the compiler) once Render
// runs. The fluent surface mutates the context post-construction (.Style(),
// XLim, Flip), so setters stay internal; true immutability waits for the
// spec/realization split.
public partial class PlotContext<T, TX, TY>
	where TX : struct
	where TY : struct
{
	internal string? Title { get; set; }

	internal string? SubTitle { get; set; }

	internal string? XLab { get; set; }

	internal string? Caption { get; set; }

	internal Selectors<T, TX, TY> Selectors { get; } = new();

	// Default scale factories, chosen at Build time where overload resolution has
	// already dispatched on TX/TY. Invoked by Init (with the coordinate system's
	// expansion hints) only when the user registered no scale.
	internal Action<Coords.ICoordinateSystem>? XScaleDefault { get; set; }

	internal Action<Coords.ICoordinateSystem>? YScaleDefault { get; set; }

	// Containers that straddle the buckets: Factory is spec (the recipe), the
	// Scales it instantiates are per-pass realizations (one per facet cell).
	internal Positions<TX, TY> Positions { get; } = new();

	internal Aesthetics<T> Aesthetics { get; } = new();

	internal Faceting<T>? Faceting { get; set; }

	public bool Flip { get; set; }

	public CoordSystem CoordSystem { get; set; } = CoordSystem.Cartesian;

	public PolarOptions PolarOptions { get; } = new();

	public Style? Style { get; set; }

	public PanelFactory<T, TX, TY>? DefaultFactory { get; set; }

	internal List<PanelFactory<T, TX, TY>> PanelFactories { get; } = [];
}
