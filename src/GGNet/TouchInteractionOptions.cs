namespace GGNet;

/// <summary>Explicit touch policy for shared Cartesian x axes. Replaces the defaults inferred from Pan and Crosshair. An empty value disables chart touch gestures.</summary>
public sealed record TouchInteractionOptions
{
	/// <summary>Chart navigation; defaults to none. Does not depend on Crosshair, Pan or the wheel's Zoom axes.</summary>
	public TouchNavigation Navigation { get; init; }

	/// <summary>Tap to pin and hold for 400 ms within 10 screen pixels to inspect; release pins. Works without rendering a crosshair.</summary>
	public bool Inspect { get; init; }

	/// <summary>Use a short tap to invoke the panel's onActivate callback instead of pinning. Hold inspection remains available. Defaults to false; never dispatches a mark click.</summary>
	public bool TapActivatesPanel { get; init; }

	/// <summary>Two taps on the same panel within 300 ms and 24 screen pixels reset the view, retaining inspection. Single-tap activation or pinning waits 300 ms when enabled. Defaults to false.</summary>
	public bool DoubleTapResetsView { get; init; }

	/// <summary>Browser ownership while either touch capability is enabled; defaults to plot-owned touch.</summary>
	public TouchOwnership Ownership { get; init; }

	internal bool Enabled => Inspect || TapActivatesPanel || DoubleTapResetsView || Navigation != TouchNavigation.None;
}
