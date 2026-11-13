namespace GGNet;

/// <summary>How a reading in the crosshair readout was obtained.</summary>
public enum ReadingKind
{
	/// <summary>The nearest sample, within the series' maximum distance.</summary>
	Observed,

	/// <summary>The last sample at or before the cursor, within the series' maximum distance.</summary>
	Previous,

	/// <summary>The last sample at or before the cursor, still in effect.</summary>
	Step,

	/// <summary>The bar whose x extent contains the cursor.</summary>
	Containing,

	/// <summary>No sample qualified; the value is the series' missing text.</summary>
	Missing
}
