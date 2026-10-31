using GGNet.Data;
using GGNet.Facets;

namespace GGNet;

public partial class PlotContext<T, TX, TY>
	where TX : struct
	where TY : struct
{
	internal string? Title { get; set; }

	internal string? SubTitle { get; set; }

	internal string? XLab { get; set; }

	internal string? Caption { get; set; }

	internal Selectors<T, TX, TY> Selectors { get; } = new();

	internal Action<Coords.CoordinateSystem>? XScaleDefault { get; set; }

	internal Action<Coords.CoordinateSystem>? YScaleDefault { get; set; }

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
