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
	// already dispatched on TX/TY. Invoked by Init (with the polar flag) only when
	// the user registered no scale.
	internal Action<bool>? XScaleDefault { get; set; }

	internal Action<bool>? YScaleDefault { get; set; }

	internal Positions<TX, TY> Positions { get; } = new();

	internal Aesthetics<T> Aesthetics { get; } = new();

	public Faceting<T>? Faceting { get; set; }

	public bool Flip { get; set; }

	public CoordSystem CoordSystem { get; set; } = CoordSystem.Cartesian;

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
    try
    {
      this.grid = grid;

      var polar = CoordSystem == CoordSystem.Polar;

      if (polar && Flip)
      {
        throw new GGNetUserException("Flip is not supported with polar coordinates");
      }

      if (Positions.X.Factory is null)
      {
        if (XScaleDefault is null)
        {
          throw new GGNetUserException("Type could not be inferred");
        }

        XScaleDefault(polar);
      }

      if (Positions.Y.Factory is null)
      {
        if (YScaleDefault is null)
        {
          throw new GGNetUserException("Type could not be inferred");
        }

        YScaleDefault(polar);
      }

      Style ??= Style.Default();

      Legends = new(Style);
    }
    finally
    {
      Initialized = true;
    }
	}

	protected void RunDefaultPanel(bool first)
	{
		if (first)
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
		else
		{
			for (var i = 0; i < Positions.X.Scales.Count; i++)
			{
				Positions.X.Scales[i].Clear();
			}

			for (var i = 0; i < Positions.Y.Scales.Count; i++)
			{
				Positions.Y.Scales[i].Clear();
			}
		}
	}

	protected void RunFaceting(bool first)
	{
		if (!first)
		{
			Faceting!.Clear();

			Panels.Clear();

			Positions.X.Scales.Clear();
			Positions.Y.Scales.Clear();
		}

		for (var i = 0; i < Source?.Count; i++)
		{
			Faceting!.Train(Source[i]);
		}

		Faceting!.Commit();

		var facets = Faceting.Facets(Style!);

		N = (Faceting.NRows, Faceting.NColumns);

		var width = 1.0 / Faceting.NColumns;
		var height = 1.0 / Faceting.NRows; ;

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

	protected void ClearAesthetics(bool first)
	{
		if (first)
		{
			return;
		}

		for (var i = 0; i < Aesthetics.Scales.Count; i++)
		{
			Aesthetics.Scales[i].Clear();
		}
	}

	protected void RunLegend()
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

	public void Render(bool first)
	{
		if (Faceting is null)
		{
			RunDefaultPanel(first);
		}
		else
		{
			RunFaceting(first);
		}

		if (first)
		{
			for (int p = 0; p < Panels.Count; p++)
			{
				var panel = Panels[p];

				for (int g = 0; g < panel.Geoms.Count; g++)
				{
					var geom = panel.Geoms[g];

					if ((geom.SupportedCoordSystems & CoordSystem) == 0)
					{
						throw new GGNetUserException($"{geom.GetType().Name.Split('`')[0]} does not support {CoordSystem} coordinates");
					}
				}
			}
		}

		ClearAesthetics(first);

		for (int p = 0; p < Panels.Count; p++)
		{
			var panel = Panels[p];

			for (int g = 0; g < panel.Geoms.Count; g++)
			{
				panel.Geoms[g].Train();
			}
		}

		for (int i = 0; i < Aesthetics.Scales.Count; i++)
		{
			Aesthetics.Scales[i].Commit(grid);
		}

		if (grid)
		{
			RunLegend();
		}

		for (var p = 0; p < Panels.Count; p++)
		{
			var panel = Panels[p];

			for (var g = 0; g < panel.Geoms.Count; g++)
			{
				var geom = panel.Geoms[g];
				if (!first)
				{
					geom.Clear();
				}

				geom.Shape(Flip);
			}
		}

		var height = 0.0;
		var xtitles = 0.0;

		for (var i = 0; i < Positions.X.Scales.Count; i++)
		{
			var scale = Positions.X.Scales[i];

			scale.Commit(grid);

			if (grid)
			{
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

		for (var i = 0; i < Positions.Y.Scales.Count; i++)
		{
			var scale = Positions.Y.Scales[i];

			scale.Commit(grid);

			if (grid)
			{
				foreach (var (_, label) in scale.Labels)
				{
					width = Max(width, label.Width(Style.Axis.Text.Y.FontSize));
				}

				foreach (var (_, title) in scale.Titles)
				{
					ytitles = Max(ytitles, title.Height(Style.Axis.Title.Y.FontSize));
				}
			}
		}

		if (CoordSystem == CoordSystem.Polar)
		{
			// Polar panels draw breaks and labels inside the plotting area;
			// no cartesian axis bands are reserved around it.
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
