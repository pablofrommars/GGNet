namespace GGNet;

/// <summary>
/// Opt-in for in-chart pointer gestures on the interactive <c>Plot</c>
/// component. When unset (the default), the plot renders exactly as a static
/// one — no capture group, no gesture handlers, byte-identical output.
/// The svg stays responsive: pointer coordinates are converted to plot units
/// client-side using the capture element's inverse screen CTM.
/// Options are fixed when the Plot component is initialized. Recreate the
/// component with a different <c>@key</c> to change them; equal option values
/// may be supplied again.
/// </summary>
public sealed record InteractivityOptions
{
	/// <summary>Axes the wheel gesture zooms; x alone is the time-series default.</summary>
	public ZoomAxis Zoom { get; init; } = ZoomAxis.X;

	/// <summary>Window shrink factor per wheel notch toward the cursor; the inverse widens on scroll-out. Between 0 and 1, closer to 1 is gentler.</summary>
	public double ZoomStep { get; init; } = 0.8;

	/// <summary>Shows a vertical crosshair with an x-value label shared across linked panels. A mouse hovers and clicks to pin. By default touch taps pin and a 400 ms hold enters inspection; immediate horizontal drag pans when Pan enables x navigation. Layers with <c>inspect:</c> report readings through <c>Plot.ReadingsChanged</c>; <c>@bind-Inspection</c> exposes the data-coordinate pin. Instant and numeric cursors are continuous; LocalDateTime and discrete cursors snap. Free-x and flipped plots retain a local mouse/pen cursor without a shared pin.</summary>
	public bool Crosshair { get; init; }

	/// <summary>Mouse and pen drag to pan along the <see cref="Zoom"/> axes. With an x zoom axis, also enables one-finger x pan and two-finger x pan/pinch by default. Crosshair does not change that navigation. Explicit <see cref="Touch"/> overrides the inferred touch policy.</summary>
	public bool Pan { get; init; }

	/// <summary>Explicit touch policy. When omitted, Pan with an x zoom axis enables pan/pinch, Crosshair enables tap/hold inspection, and active gestures own the plot area. Unsupported layouts disable inferred shared touch; explicit enabled requests require shared Cartesian x axes. Use ScrollY to permit native vertical scrolling, or an empty value to disable chart touch gestures.</summary>
	public TouchInteractionOptions? Touch { get; init; }

	internal TouchInteractionOptions ResolveTouch(bool supported)
	{
		if (Touch is { } requested)
		{
			if (!Enum.IsDefined(requested.Navigation) || !Enum.IsDefined(requested.Ownership))
			{
				throw new Exceptions.GGNetUserException("Touch navigation and ownership must be defined values.");
			}

			if (requested.Enabled && !supported)
			{
				throw new Exceptions.GGNetUserException("Explicit touch interaction requires an unflipped Cartesian plot with one shared x scale and no free-x facets.");
			}

			return requested;
		}

		return new()
		{
			Navigation = supported && Pan && Zoom is ZoomAxis.X or ZoomAxis.Both ? TouchNavigation.PanAndPinchX : TouchNavigation.None,
			Inspect = supported && Crosshair
		};
	}

	/// <summary>Y follows the x window: every x view change (wheel, pan, host commands) refits the y axis to the data visible inside the window, with a small margin. Y becomes derived state — y gestures are disabled and manual y windows are overwritten on the next x change.</summary>
	public bool AutoFitY { get; init; }

	/// <summary>Renders mark tooltips as a top-layer popover glued to the cursor: position and edge-flipping run client-side at frame rate, while content stays server-rendered per mark. Without it, tooltips keep the classic mark-anchored placement.</summary>
	public bool CursorTooltip { get; init; }
}
