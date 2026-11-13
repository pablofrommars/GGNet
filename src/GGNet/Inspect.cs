using GGNet.Exceptions;

namespace GGNet;

/// <summary>
/// Joins a layer to the crosshair readout of an interactive plot. Passed as
/// <c>inspect:</c> to <c>Geom_Line</c>, <c>Geom_Point</c>, <c>Geom_Area</c> or
/// <c>Geom_Bar</c>. It changes nothing that is drawn.
/// </summary>
/// <typeparam name="T">The layer's row type.</typeparam>
/// <param name="Name">Series name shown in the readout.</param>
/// <param name="Value">Text of a reading, from the row it was read from. Format numbers invariantly or through an <c>IFormatter</c>.</param>
/// <param name="Policy">How the reading at the cursor is looked up.</param>
public sealed record Inspect<T>(string Name, Func<T, string> Value, InspectPolicy Policy = InspectPolicy.Nearest)
{
	/// <summary>Series name shown in the readout.</summary>
	public string Name { get; init; } = !string.IsNullOrWhiteSpace(Name)
		? Name
		: throw new GGNetUserException("inspect: a series name is required");

	/// <summary>Text of a reading, from the row it was read from.</summary>
	public Func<T, string> Value { get; init; } = Value
		?? throw new GGNetUserException("inspect: a value formatter is required");

	/// <summary>Farthest a sample may be from the cursor on an <c>Instant</c> x axis and still be read under <c>Nearest</c> or <c>Previous</c>. Null sets no limit.</summary>
	public Duration? MaxDuration { get; init; }

	/// <summary>Farthest a sample may be from the cursor on a numeric x axis, in x-axis data units, and still be read under <c>Nearest</c> or <c>Previous</c>. Null sets no limit.</summary>
	public double? MaxDistance { get; init; }

	/// <summary>Text of the reading when no sample qualifies.</summary>
	public string Missing { get; init; } = "No nearby reading";

	/// <summary>Display name of a reading when the layer is split by <c>colorBy</c>, <c>lineTypeBy</c> or <c>fillBy</c>, from the group's legend label. Null uses the label itself.</summary>
	public Func<string, string>? GroupName { get; init; }

	internal Inspect<T> RequireSampled(string geom)
		=> Policy is InspectPolicy.Containing
			? throw new GGNetUserException($"inspect: {nameof(InspectPolicy)}.{nameof(InspectPolicy.Containing)} reads bars; {geom} takes Nearest, Previous or Step")
			: this;
}
