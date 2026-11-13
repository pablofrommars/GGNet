namespace GGNet;

/// <summary>One series' reading at the crosshair position of an interactive plot.</summary>
/// <typeparam name="TX">The plot's x type.</typeparam>
/// <param name="Series">The name given to the layer's <c>inspect:</c>.</param>
/// <param name="Group">The legend label of the layer's color, line-type or fill group the reading belongs to; null when the layer is not split.</param>
/// <param name="Name">Display name: the group when the layer is split, the series name otherwise, or whatever <c>GroupName</c> returns.</param>
/// <param name="Cursor">The inspected x value, the same for every reading of one inspection.</param>
/// <param name="Sample">The x value of the sample the reading was taken from; null when missing.</param>
/// <param name="Kind">How the reading was obtained.</param>
/// <param name="Value">The formatted value, or the series' missing text.</param>
public sealed record Reading<TX>(string Series, string? Group, string Name, TX Cursor, TX? Sample, ReadingKind Kind, string Value)
	where TX : struct;
