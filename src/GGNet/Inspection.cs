namespace GGNet;

/// <summary>The crosshair's position on an interactive plot: one x value shared by every panel. Bind it with <c>@bind-Inspection</c> to read it, set it or clear it (null) from the host.</summary>
/// <typeparam name="TX">The plot's x type.</typeparam>
/// <param name="X">The inspected x value, in data units. Never a pixel or an index: new data, resizing and view changes do not move it.</param>
/// <param name="Pinned">True once a tap, a click, a finished drag or the host has fixed the position; false while a mouse hover or a drag in progress is only previewing it.</param>
public sealed record Inspection<TX>(TX X, bool Pinned = true)
	where TX : struct;
