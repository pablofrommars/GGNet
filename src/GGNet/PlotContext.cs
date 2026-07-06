using GGNet.Buffers;
using GGNet.Data;
using GGNet.Exceptions;
using GGNet.Facets;

using static System.Math;

namespace GGNet;

public partial class PlotContext<T, TX, TY> : IPlotContext
	where TX : struct
	where TY : struct
{
	public PlotContext()
	{
		Id = "gg" + Convert.ToBase64String(BitConverter.GetBytes(GetHashCode()))[0..^2].Replace('+', '-').Replace('/', '_');
	}

	public string Id { get; }

	public IReadOnlyList<T>? Source { get; init; }

	internal IReadOnlyList<T> RequireSource()
		=> Source ?? throw new GGNetUserException("This plot was built without a source; use the Geom_Xxx overloads that take one");

	internal bool Initialized { get; set; }

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

	internal Positions<TX, TY> Positions { get; } = new();

	internal Aesthetics<T> Aesthetics { get; } = new();

	internal Faceting<T>? Faceting { get; set; }

	public bool Flip { get; set; }

	public CoordSystem CoordSystem { get; set; } = CoordSystem.Cartesian;

	// The plot-level strategy instance answers plot-level policy (axis bands,
	// expansion hints); panels materialize their own measured instances.
	internal Coords.ICoordinateSystem Coord { get; private set; } = default!;

	internal Coords.ICoordinateSystem MakeCoordinateSystem()
	{
		if (CoordSystem == CoordSystem.Polar)
		{
			if (Flip)
			{
				throw new GGNetUserException("Flip is not supported with polar coordinates");
			}

			return new Coords.PolarCoordinateSystem(PolarOptions, Style!);
		}

		return new Coords.CartesianCoordinateSystem(Style!);
	}

	public PolarOptions PolarOptions { get; } = new();

	public Style? Style { get; set; }

	public PanelFactory<T, TX, TY>? DefaultFactory { get; set; }

	public Buffer<PanelFactory<T, TX, TY>> PanelFactories { get; } = new(4, 1);

	internal Buffer<Panel<T, TX, TY>> Panels { get; } = new(16, 1);

	internal Legends Legends { get; set; } = default!;

	internal (int rows, int cols) N { get; set; }

	internal double Strip { get; set; }

	internal (double width, double height) Axis { get; set; }

	internal (bool x, bool y) AxisVisibility { get; set; }

	internal (double x, double y) AxisTitles { get; set; }

	internal (bool x, bool y) AxisTitlesVisibility { get; set; }

	private bool grid = true;

	public Type PlotType => typeof(Components.Plot<T, TX, TY>);

	public void Init(bool grid = true)
	{
		if (Initialized)
		{
			return;
		}

		this.grid = grid;

		Style ??= Style.Default();

		Coord = MakeCoordinateSystem();

		if (Positions.X.Factory is null)
		{
			if (XScaleDefault is null)
			{
				throw new GGNetUserException("Type could not be inferred");
			}

			XScaleDefault(Coord);
		}

		if (Positions.Y.Factory is null)
		{
			if (YScaleDefault is null)
			{
				throw new GGNetUserException("Type could not be inferred");
			}

			YScaleDefault(Coord);
		}

		Legends = new(Style);

		Initialized = true;
	}

	private void BuildDefaultPanels()
	{
		{
			var n = PanelFactories.Count;

			if (n > 0)
			{
				N = (n, 1);

				Positions.X.Instance();

				var ylab = 0.0;

				for (var i = 0; i < n; i++)
				{
					var lab = PanelFactories[i].YLab;
					if (!string.IsNullOrEmpty(lab))
					{
						ylab = lab.Height(Style!.Axis.Title.Y.FontSize);

						break;
					}
				}

				for (var i = 0; i < n; i++)
				{
					var factory = PanelFactories[i];

					if (factory.Y is null)
					{
						Positions.Y.Instance();
					}
					else
					{
						Positions.Y.Register(factory.Y());
					}

					var panel = factory.Build((i, 0));

					if (i == (n - 1))
					{
						panel.Axis = (true, true);

						if (!string.IsNullOrEmpty(XLab))
						{
							panel.XLab = (XLab.Height(Style!.Axis.Title.X.FontSize), XLab);
						}
					}
					else
					{
						panel.Axis = (false, true);
					}

					panel.YLab = (ylab, factory.YLab);

					Panels.Add(panel);
				}
			}
			else if (DefaultFactory is not null)
			{
				N = (1, 1);

				Positions.X.Instance();
				Positions.Y.Instance();

				var panel = DefaultFactory.Build((0, 0));

				panel.Axis = (true, true);

				if (!string.IsNullOrEmpty(XLab))
				{
					panel.XLab = (XLab.Height(Style!.Axis.Title.X.FontSize), XLab);
				}

				if (!string.IsNullOrEmpty(DefaultFactory.YLab))
				{
					panel.YLab = (DefaultFactory.YLab.Height(Style!.Axis.Title.Y.FontSize), DefaultFactory.YLab);
				}

				Panels.Add(panel);
			}
		}
	}

	private void BuildFacetPanels()
	{
		for (var i = 0; i < Source?.Count; i++)
		{
			Faceting!.Train(Source[i]);
		}

		Faceting!.Commit();

		var facets = Faceting.Facets(Style!);

		N = (Faceting.NRows, Faceting.NColumns);

		var width = 1.0 / Faceting.NColumns;
		var height = 1.0 / Faceting.NRows;

		if (Faceting.Strip)
		{
			Strip = Style!.Strip.Text.X.FontSize.Height();
		}

		if (!Faceting.FreeX)
		{
			Positions.X.Instance();
		}

		if (!Faceting.FreeY)
		{
			Positions.Y.Instance();
		}

		AxisVisibility = (Faceting.FreeX, Faceting.FreeY);

		var xlab = 0.0;
		if (!string.IsNullOrEmpty(XLab))
		{
			xlab = XLab.Height(Style!.Axis.Title.X.FontSize);
		}

		var ylab = 0.0;
		if (!string.IsNullOrEmpty(DefaultFactory!.YLab))
		{
			ylab = DefaultFactory.YLab.Height(Style!.Axis.Title.Y.FontSize);
		}

		for (var i = 0; i < facets.Length; i++)
		{
			var (facet, showX, showY) = facets[i];

			if (Faceting.FreeX)
			{
				Positions.X.Instance();
			}

			if (Faceting.FreeY)
			{
				Positions.Y.Instance();
			}

			var panel = DefaultFactory.Build(facet.Coord, facet, width, height);

			panel.Strip = (facet.XStrip, facet.YStrip);

			panel.Axis = (Faceting.FreeX || showX, Faceting.FreeY || showY);

			if (xlab > 0.0 && facet.Coord.row == (Faceting.NRows - 1))
			{
				if (facet.Coord.column == (Faceting.NColumns - 1))
				{
					panel.XLab = (xlab, XLab);
				}
				else
				{
					panel.XLab = (xlab, null);
				}
			}

			if (ylab > 0)
			{
				if (Style!.Axis.Y == Position.Left && panel.Coord.col == 0)
				{
					if (panel.Coord.row == 0)
					{
						panel.YLab = (ylab, DefaultFactory.YLab);
					}
					else
					{
						panel.YLab = (ylab, null);
					}
				}
				else if (Style!.Axis.Y == Position.Right && panel.Coord.col == (Faceting.NColumns - 1))
				{
					if (panel.Coord.row == 0)
					{
						panel.YLab = (ylab, DefaultFactory.YLab);
					}
					else
					{
						panel.YLab = (ylab, null);
					}
				}
			}

			Panels.Add(panel);
		}
	}

	private void BuildLegends()
	{
		if (Faceting is null)
		{
			for (int p = 0; p < Panels.Count; p++)
			{
				var panel = Panels[p];

				for (int g = 0; g < panel.Geoms.Count; g++)
				{
					panel.Geoms[g].Legend();
				}
			}
		}
		else
		{
			var panel = Panels[0];

			for (int g = 0; g < panel.Geoms.Count; g++)
			{
				panel.Geoms[g].Legend();
			}
		}
	}

	// The render pipeline. Flagless and idempotent by construction: Reset clears
	// every stateful participant unconditionally (clearing empty state is free),
	// EnsurePanels builds only what is absent, and no stage depends on whether a
	// previous pass ran. Rendering twice yields identical output.
	public void Render()
	{
		if (!Initialized)
		{
			Init(grid);
		}

		Reset();
		EnsurePanels();
		Validate();
		Train();
		CommitAesthetics();

		if (grid)
		{
			BuildLegends();
		}

		Shape();
		CommitPositions();
		MeasureAxes();
	}

	private void Reset()
	{
		for (var i = 0; i < Positions.X.Scales.Count; i++)
		{
			Positions.X.Scales[i].Clear();
		}

		for (var i = 0; i < Positions.Y.Scales.Count; i++)
		{
			Positions.Y.Scales[i].Clear();
		}

		for (var i = 0; i < Aesthetics.Scales.Count; i++)
		{
			Aesthetics.Scales[i].Clear();
		}

		for (var p = 0; p < Panels.Count; p++)
		{
			var panel = Panels[p];

			for (var g = 0; g < panel.Geoms.Count; g++)
			{
				panel.Geoms[g].Clear();
			}
		}

		Faceting?.Clear();

		// Clear contents, never replace the instance: geoms capture the container
		// reference at panel-build time, and default-path panels outlive passes.
		Legends.Clear();
	}

	private void EnsurePanels()
	{
		if (Faceting is null)
		{
			if (Panels.Count == 0)
			{
				BuildDefaultPanels();
			}
		}
		else
		{
			// The panel set is data-dependent under faceting: re-derive it every pass.
			Panels.Clear();

			Positions.X.Scales.Clear();
			Positions.Y.Scales.Clear();

			BuildFacetPanels();
		}
	}

	private void Validate()
	{
		for (var p = 0; p < Panels.Count; p++)
		{
			var panel = Panels[p];

			for (var g = 0; g < panel.Geoms.Count; g++)
			{
				var geom = panel.Geoms[g];

				if ((geom.SupportedCoordSystems & CoordSystem) == 0)
				{
					throw new GGNetUserException($"{geom.GetType().Name.Split('`')[0]} does not support {CoordSystem} coordinates");
				}
			}
		}
	}

	private void Train()
	{
		for (var p = 0; p < Panels.Count; p++)
		{
			var panel = Panels[p];

			for (var g = 0; g < panel.Geoms.Count; g++)
			{
				panel.Geoms[g].Train();
			}
		}
	}

	private void CommitAesthetics()
	{
		for (var i = 0; i < Aesthetics.Scales.Count; i++)
		{
			Aesthetics.Scales[i].Commit(grid);
		}
	}

	private void Shape()
	{
		for (var p = 0; p < Panels.Count; p++)
		{
			var panel = Panels[p];

			for (var g = 0; g < panel.Geoms.Count; g++)
			{
				panel.Geoms[g].Shape(Flip);
			}
		}
	}

	private void CommitPositions()
	{
		for (var i = 0; i < Positions.X.Scales.Count; i++)
		{
			Positions.X.Scales[i].Commit(grid);
		}

		for (var i = 0; i < Positions.Y.Scales.Count; i++)
		{
			Positions.Y.Scales[i].Commit(grid);
		}
	}

	private void MeasureAxes()
	{
		var height = 0.0;
		var xtitles = 0.0;

		if (grid)
		{
			for (var i = 0; i < Positions.X.Scales.Count; i++)
			{
				var scale = Positions.X.Scales[i];

				foreach (var (_, label) in scale.Labels)
				{
					height = Max(height, label.Height(Style!.Axis.Text.X.FontSize));
				}

				foreach (var (_, title) in scale.Titles)
				{
					xtitles = Max(xtitles, title.Height(Style!.Axis.Title.X.FontSize));
				}
			}
		}

		var xtitlesVisibility = xtitles > 0.0;

		xtitles = Max(xtitles, XLab.Height(Style!.Axis.Title.X.FontSize));

		var width = 0.0;
		var ytitles = 0.0;

		if (grid)
		{
			for (var i = 0; i < Positions.Y.Scales.Count; i++)
			{
				var scale = Positions.Y.Scales[i];

				foreach (var (_, label) in scale.Labels)
				{
					width = Max(width, label.Width(Style!.Axis.Text.Y.FontSize));
				}

				foreach (var (_, title) in scale.Titles)
				{
					ytitles = Max(ytitles, title.Height(Style!.Axis.Title.Y.FontSize));
				}
			}
		}

		if (!Coord.CarvesAxisBands)
		{
			// Breaks and labels live inside the plotting area;
			// no axis bands are reserved around it.
			Axis = (0.0, 0.0);

			AxisTitles = (0.0, 0.0);

			AxisTitlesVisibility = (false, false);
		}
		else
		{
			Axis = (width, height);

			AxisTitles = (xtitles, ytitles);

			AxisTitlesVisibility = (xtitlesVisibility, false);
		}
	}
}
