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
		builder.AddAttribute(2, "data-ggnet-inspection", frame.InspectionStamp);
		builder.AddAttribute(3, "data-ggnet-selection", frame.Selection is { } selected ? Num(selected) : null);
		builder.AddAttribute(4, "data-ggnet-committed", frame.CommittedSelection is { } committed ? Num(committed) : null);
		if (!frame.Draw)
		{
			builder.CloseElement();
			return;
		}

		builder.OpenElement(5, "g");
		builder.AddAttribute(6, "data-ggnet-cursor", string.Empty);
		builder.AddAttribute(7, "data-ggnet-pinned", frame.Pinned ? string.Empty : null);
		builder.AddAttribute(8, "visibility", frame.Visible ? null : "hidden");
		builder.AddAttribute(9, "data-ggnet-view", frame.View);

		builder.OpenElement(10, "line");
		builder.AddAttribute(11, "class", frame.Pinned ? "crosshair pinned pointer-events-none" : "crosshair pointer-events-none");
		builder.AddAttribute(12, "x1", Num(frame.X));
		builder.AddAttribute(13, "x2", Num(frame.X));
		builder.AddAttribute(14, "y1", Num(frame.Top));
		builder.AddAttribute(15, "y2", Num(frame.Bottom));
		builder.CloseElement();

		builder.OpenElement(16, "text");
		builder.AddAttribute(18, "class", "crosshair-label pointer-events-none");
		builder.AddAttribute(19, "x", Num(frame.X));
		builder.AddAttribute(20, "y", Num(frame.Top + 14.0));
		builder.AddAttribute(21, "text-anchor", "middle");
		builder.AddAttribute(22, "font-size", FontSize);
		builder.AddContent(23, frame.Label);
		builder.CloseElement();

		builder.CloseElement();

		builder.OpenElement(24, "g");
		builder.AddAttribute(25, "data-ggnet-cursor-markers", string.Empty);

		for (var i = 0; i < frame.Markers.Count; i++)
		{
			builder.OpenElement(26, "circle");
			builder.AddAttribute(27, "class", "crosshair-marker pointer-events-none");
			builder.AddAttribute(28, "cx", Num(frame.Markers[i].x));
			builder.AddAttribute(29, "cy", Num(frame.Markers[i].y));
			builder.AddAttribute(30, "r", "4");
			builder.CloseElement();
		}

		builder.CloseElement();

		builder.CloseElement();
	}
}
