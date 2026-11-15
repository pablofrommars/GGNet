namespace GGNet;

/// <summary>Explicit touch policy for shared Cartesian x axes. Replaces the defaults inferred from Pan and Crosshair. An empty value disables chart touch gestures.</summary>
public sealed record TouchInteractionOptions
{
	/// <summary>Chart navigation; defaults to none. Does not depend on Crosshair, Pan or the wheel's Zoom axes.</summary>
	public TouchNavigation Navigation { get; init; }

	/// <summary>Tap to pin and hold for 400 ms within 10 screen pixels to inspect; release pins. Works without rendering a crosshair.</summary>
	public bool Inspect { get; init; }

	/// <summary>Browser ownership while either touch capability is enabled; defaults to plot-owned touch.</summary>
	public TouchOwnership Ownership { get; init; }

	internal bool Enabled => Inspect || Navigation != TouchNavigation.None;
}
