namespace GGNet;

/// <summary>
/// What a refresh asks of the plot: re-render the composed output, or only surface the loading state.
/// </summary>
public enum RenderTarget
{
	Render = 0,
	Loading = 1
}
