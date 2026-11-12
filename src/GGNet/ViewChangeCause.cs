namespace GGNet;

/// <summary>What changed a plot's view window.</summary>
public enum ViewChangeCause
{
	/// <summary>A pointer gesture on the chart: wheel zoom or drag pan.</summary>
	Gesture,

	/// <summary>Host code: <c>ZoomToXAsync</c>, <c>ZoomToYAsync</c>, <c>ResetViewAsync</c>, <c>ShowLastAsync</c>, or a window set on the context and then refreshed.</summary>
	HostCommand,

	/// <summary>The reset gesture on the chart: a double click or double tap.</summary>
	Reset
}
