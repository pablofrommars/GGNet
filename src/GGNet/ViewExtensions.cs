using GGNet.Components;
using GGNet.Exceptions;

namespace GGNet;

// Time-axis view commands, surfaced only on the closed generic instantiations
// where a trailing window is meaningful (Instant x). State lives on
// PlotContext (SetXView); these are typed composites over it.
public static class ViewExtensions
{
	/// <summary>Windows the x axis to the trailing span of the plot's source — "show the last 48 hours". Anchored at the latest x value in the source; survives re-renders until the view is reset.</summary>
	/// <param name="context">The plot whose x axis to window.</param>
	/// <param name="window">Trailing window length, ending at the latest x value in the source.</param>
	public static void ShowLast<T, TY>(this PlotContext<T, Instant, TY> context, Duration window)
		where TY : struct
	{
		var x = context.Selectors.X ?? throw new GGNetUserException("ShowLast requires the default x selector supplied to Build");

		var source = context.RequireSource();

		if (source.Count == 0)
		{
			return;
		}

		var end = x(source[0]);

		for (var i = 1; i < source.Count; i++)
		{
			var value = x(source[i]);

			if (value > end)
			{
				end = value;
			}
		}

		context.SetXView(end - window, end);
	}

	/// <summary>Windows the x axis to the trailing span of the data and re-renders — "show the last 48 hours". Anchored at the latest x value in the source; survives re-renders until the view is reset.</summary>
	/// <param name="plot">The plot component to window and refresh.</param>
	/// <param name="window">Trailing window length, ending at the latest x value in the source.</param>
	/// <param name="token">Cancels the refresh hand-off.</param>
	public static Task ShowLastAsync<T, TY>(this Plot<T, Instant, TY> plot, Duration window, CancellationToken token = default)
		where TY : struct
	{
		plot.Context.ShowLast(window);

		if (plot.Interactivity is { AutoFitY: true })
		{
			plot.Context.FitYToXView();
		}

		return plot.RefreshAsync(RenderTarget.Render, token);
	}
}
