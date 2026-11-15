namespace GGNet;

/// <summary>Navigation gestures for a shared Cartesian x axis, independent of inspection and mouse pan.</summary>
public enum TouchNavigation
{
	/// <summary>No chart navigation by touch.</summary>
	None,

	/// <summary>Two fingers translate and pinch the x window; one finger does not pan.</summary>
	TwoFingerX,

	/// <summary>One finger pans horizontally; two fingers translate and pinch the x window.</summary>
	PanAndPinchX
}
