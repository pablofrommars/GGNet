namespace GGNet;

/// <summary>The readout of an inspection, raised through <c>Plot.ReadingsChanged</c> whenever it changes: when the crosshair moves, and when data changes under a position that stays where it is.</summary>
/// <typeparam name="TX">The plot's x type.</typeparam>
/// <param name="Inspection">The position the readings belong to; null once the inspection is cleared.</param>
/// <param name="Values">One reading per inspected series, in panel then layer order. Empty when cleared or when no layer has <c>inspect:</c>.</param>
/// <param name="OutsideView">True when the position lies outside the visible x window: its line is hidden, the readings still hold.</param>
public sealed record InspectionReadings<TX>(Inspection<TX>? Inspection, IReadOnlyList<Reading<TX>> Values, bool OutsideView)
	where TX : struct
{
	/// <summary>Value equality, comparing the readings one by one.</summary>
	public bool Equals(InspectionReadings<TX>? other)
		=> other is not null
			&& EqualityComparer<Inspection<TX>?>.Default.Equals(Inspection, other.Inspection)
			&& OutsideView == other.OutsideView
			&& Values.SequenceEqual(other.Values);

	/// <inheritdoc/>
	public override int GetHashCode() => HashCode.Combine(Inspection, OutsideView, Values.Count);
}
