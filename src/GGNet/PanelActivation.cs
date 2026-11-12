namespace GGNet;

/// <summary>Identifies the sub-panel a panel activation came from.</summary>
/// <param name="Index">Zero-based position of the sub-panel among the plot's <c>Panel(...)</c> calls.</param>
/// <param name="Key">The key given to that <c>Panel(...)</c> call, or null.</param>
public sealed record PanelActivation(int Index, object? Key);
