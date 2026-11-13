namespace GGNet;

/// <summary>How an inspected series finds its reading at the crosshair position. Chosen per series; never inferred from how the series is drawn.</summary>
public enum InspectPolicy
{
	/// <summary>The sample closest to the cursor on either side, within the series' maximum distance.</summary>
	Nearest,

	/// <summary>The last sample at or before the cursor, within the series' maximum distance.</summary>
	Previous,

	/// <summary>The last sample at or before the cursor, however old: its value stays in effect until the next sample.</summary>
	Step,

	/// <summary>The bar whose x extent contains the cursor. <c>Geom_Bar</c> only.</summary>
	Containing
}
