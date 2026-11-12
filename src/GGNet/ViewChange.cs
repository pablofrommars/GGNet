namespace GGNet;

/// <summary>A committed change of a plot's view window, raised through <c>Plot.ViewChanged</c>.</summary>
/// <param name="Cause">What changed the window.</param>
/// <param name="X">The x window after the change, in data units, lower bound first. Null when no x window is set, and on discrete and <c>LocalDateTime</c> axes.</param>
/// <param name="Y">The y window after the change, in data units, lower bound first. Null when no y window is set, and on discrete axes.</param>
public sealed record ViewChange<TX, TY>(ViewChangeCause Cause, (TX min, TX max)? X, (TY min, TY max)? Y)
	where TX : struct
	where TY : struct;
