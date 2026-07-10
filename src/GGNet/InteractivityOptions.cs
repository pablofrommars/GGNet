namespace GGNet;

/// <summary>
/// Opt-in for in-chart pointer gestures on the interactive <c>Plot</c>
/// component. When unset (the default), the plot renders exactly as a static
/// one — no capture group, no gesture handlers, byte-identical output.
/// The svg stays responsive: pointer coordinates are converted to plot units
/// client-side against the rendered size.
/// </summary>
public sealed record InteractivityOptions
{
	/// <summary>Axes the wheel gesture zooms; x alone is the time-series default.</summary>
	public ZoomAxis Zoom { get; init; } = ZoomAxis.X;

	/// <summary>Window shrink factor per wheel notch toward the cursor; the inverse widens on scroll-out. Between 0 and 1, closer to 1 is gentler.</summary>
	public double ZoomStep { get; init; } = 0.8;

	/// <summary>Shows a vertical crosshair with an x-value readout as the pointer moves over the panel — snapped to invisible hover strips, and to categories on discrete axes.</summary>
	public bool Crosshair { get; init; }

	/// <summary>Drag to pan along the <see cref="Zoom"/> axes. The drag previews as a client-side transform at frame rate and commits as a view-window shift on release.</summary>
	public bool Pan { get; init; }

	/// <summary>Y follows the x window: every x view change (wheel, pan, host commands) refits the y axis to the data visible inside the window, with a small margin. Y becomes derived state — y gestures are disabled and manual y windows are overwritten on the next x change.</summary>
	public bool AutoFitY { get; init; }

	/// <summary>Renders mark tooltips as a top-layer popover glued to the cursor: position and edge-flipping run client-side at frame rate, while content stays server-rendered per mark. Without it, tooltips keep the classic mark-anchored placement.</summary>
	public bool CursorTooltip { get; init; }
}
