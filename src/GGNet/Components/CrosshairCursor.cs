namespace GGNet.Components;

using static SvgFormat;

internal sealed class CrosshairCursor : ComponentBase
{
	[Parameter]
	public required Func<CursorFrame> Source { get; set; }

	[Parameter]
	public required string Clip { get; set; }

	[Parameter]
	public required object FontSize { get; set; }

	public void Refresh() => _ = InvokeAsync(StateHasChanged);

	protected override void BuildRenderTree(RenderTreeBuilder builder)
	{
		var frame = Source();
		var clip = $"url(#{Clip})";

		// INVARIANT: the browser module previews a move by translating the cursor group, and a clip
		// path on a translated element travels with it; the clip therefore sits one level above.
		builder.OpenElement(0, "g");
		builder.AddAttribute(1, "clip-path", clip);

		builder.OpenElement(2, "g");
		builder.AddAttribute(3, "data-ggnet-cursor", string.Empty);
		builder.AddAttribute(4, "data-ggnet-pinned", frame.Pinned ? string.Empty : null);
		builder.AddAttribute(5, "visibility", frame.Visible ? null : "hidden");

		builder.OpenElement(6, "line");
		builder.AddAttribute(7, "class", frame.Pinned ? "crosshair pinned pointer-events-none" : "crosshair pointer-events-none");
		builder.AddAttribute(8, "x1", Num(frame.X));
		builder.AddAttribute(9, "x2", Num(frame.X));
		builder.AddAttribute(10, "y1", Num(frame.Top));
		builder.AddAttribute(11, "y2", Num(frame.Bottom));
		builder.CloseElement();

		builder.OpenElement(12, "text");
		builder.AddAttribute(14, "class", "crosshair-label pointer-events-none");
		builder.AddAttribute(15, "x", Num(frame.X));
		builder.AddAttribute(16, "y", Num(frame.Top + 14.0));
		builder.AddAttribute(17, "text-anchor", "middle");
		builder.AddAttribute(18, "font-size", FontSize);
		builder.AddContent(19, frame.Label);
		builder.CloseElement();

		builder.CloseElement();

		builder.OpenElement(20, "g");
		builder.AddAttribute(21, "data-ggnet-cursor-markers", string.Empty);

		for (var i = 0; i < frame.Markers.Count; i++)
		{
			builder.OpenElement(22, "circle");
			builder.AddAttribute(23, "class", "crosshair-marker pointer-events-none");
			builder.AddAttribute(24, "cx", Num(frame.Markers[i].x));
			builder.AddAttribute(25, "cy", Num(frame.Markers[i].y));
			builder.AddAttribute(26, "r", "4");
			builder.CloseElement();
		}

		builder.CloseElement();

		builder.CloseElement();
	}
}
