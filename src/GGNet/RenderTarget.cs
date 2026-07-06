namespace GGNet;

// What a refresh asks of the plot: re-render the composed output, or just
// surface the loading state. The former Data/Theme/All flag vocabulary was
// granularity nothing consumed — measurement showed a full recompose costs
// single-digit milliseconds at realistic sizes, so re-render is one thing.
public enum RenderTarget
{
	Render = 0,
	Loading = 1
}
