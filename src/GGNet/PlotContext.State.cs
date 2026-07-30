using GGNet.Exceptions;

namespace GGNet;

// Interaction bucket: durable view-window state, written by host commands
// (and later by pointer gestures) — never authored by the fluent chain and
// never cleared by the per-pass Reset(). Commands speak data units (TX/TY);
// pixels enter only through the inverse projection seam.
public partial class PlotContext<T, TX, TY>
	where TX : struct
	where TY : struct
{
	/// <summary>Windows the x axis to an explicit data-unit range on the next render. The window shows exactly this range (no expansion) and survives re-renders until <see cref="ResetView"/> clears it.</summary>
	/// <param name="min">Window start, in x data units.</param>
	/// <param name="max">Window end, in x data units.</param>
	public void SetXView(TX min, TX max) => Positions.X.SetView(MapView(Positions.X, min, max));

	/// <summary>Windows the y axis to an explicit data-unit range on the next render. The window shows exactly this range (no expansion) and survives re-renders until <see cref="ResetView"/> clears it.</summary>
	/// <param name="min">Window start, in y data units.</param>
	/// <param name="max">Window end, in y data units.</param>
	public void SetYView(TY min, TY max) => Positions.Y.SetView(MapView(Positions.Y, min, max));

	/// <summary>Clears both axis windows; the next render falls back to the authored limits or the trained data extent.</summary>
	public void ResetView()
	{
		Positions.X.SetView(null);
		Positions.Y.SetView(null);
	}

	/// <summary>Windows the y axis to what is visible inside the current x window, with a small margin — "auto-fit y". Measures the drawn geom layers (bar baselines, ribbon bands and layered sources fit exactly; statistics stay frozen), falling back to the default source and selectors before the first render. No effect when nothing is measurable or nothing falls inside the window. Interactive plots apply it automatically after every x view change when <see cref="InteractivityOptions.AutoFitY"/> is set; hosts can call it directly (a "fit y" button, or a windowed-and-fitted headless export).</summary>
	public void FitYToXView()
	{
		var window = Positions.X.View ?? (double.NegativeInfinity, double.PositiveInfinity);

		var y = (min: double.PositiveInfinity, max: double.NegativeInfinity);

		// Measure what's drawn: the layers hold the previous pass's shapes,
		// which is exact at commit time — only the window changed.
		for (var p = 0; p < Panels.Count; p++)
		{
			var geoms = Panels[p].Geoms;

			for (var g = 0; g < geoms.Count; g++)
			{
				var (gmin, gmax) = Shapes.ShapeExtents.VisibleY(geoms[g].Layer, window);

				y = (Math.Min(y.min, gmin), Math.Max(y.max, gmax));
			}
		}

		if (y.min > y.max)
		{
			// Nothing drawn yet (pre-render, headless export): fall back to
			// the default source through the default selectors.
			y = VisibleYFromSource(window);
		}

		// Nothing visible: keep the previous y window rather than collapse.
		if (y.min > y.max)
		{
			return;
		}

		// ViewRange is exact by design; the fit brings its own margin so the
		// extremes don't sit on the panel edge. Degenerate spans pad like
		// SetRange does.
		var pad = y.min == y.max ? 0.05 : 0.05 * (y.max - y.min);

		Positions.Y.SetView((y.min - pad, y.max + pad));
	}

	private (double min, double max) VisibleYFromSource((double min, double max) window)
	{
		var y = (min: double.PositiveInfinity, max: double.NegativeInfinity);

		if (Source is null || Selectors.X is not { } x || Selectors.Y is not { } yselector)
		{
			return y;
		}

		if (Positions.X.Scales.Count == 0 && Positions.X.Factory is null)
		{
			// Defaulted scales receive their factory in Init; idempotent.
			Init();
		}

		var xscale = Positions.X.Scales.Count > 0 ? Positions.X.Scales[0] : Positions.X.Factory?.Invoke();
		var yscale = Positions.Y.Scales.Count > 0 ? Positions.Y.Scales[0] : Positions.Y.Factory?.Invoke();

		if (xscale is null || yscale is null)
		{
			return y;
		}

		for (var i = 0; i < Source.Count; i++)
		{
			var item = Source[i];

			var xv = xscale.Map(x(item));

			if (double.IsNaN(xv) || xv < window.min || xv > window.max)
			{
				continue;
			}

			var yv = yscale.Map(yselector(item));

			if (double.IsNaN(yv))
			{
				continue;
			}

			y = (Math.Min(y.min, yv), Math.Max(y.max, yv));
		}

		return y;
	}

	// Data units map into the scale's double space through a live scale when
	// one exists; before the first render a throwaway factory instance serves,
	// which is exact for continuous/instant scales (their Map is stateless).
	// Discrete scales map through their trained values, so they need a render
	// first — the NaN guard below turns that into an actionable error.
	private (double min, double max) MapView<TKey>(Data.Position<TKey> position, TKey min, TKey max)
		where TKey : struct
	{
		if (position.Scales.Count == 0 && position.Factory is null)
		{
			// Defaulted scales receive their factory in Init; idempotent.
			Init();
		}

		var scale = position.Scales.Count > 0
			? position.Scales[0]
			: position.Factory!();

		var view = (min: scale.Map(min), max: scale.Map(max));

		if (double.IsNaN(view.min) || double.IsNaN(view.max))
		{
			throw new GGNetUserException("View bounds could not be mapped; on discrete axes render once before setting a view");
		}

		return view;
	}
}
