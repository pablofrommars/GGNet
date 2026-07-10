namespace GGNet.Components;

public abstract class TooltipBase : ComponentBase, ITooltip
{
	[Parameter]
	public required ICoord Coord { get; set; }

	[Parameter]
	public Zone Zone { get; set; }

	// Cursor-glued mode: the host renders the bubble as a top-layer popover
	// and GlueAsync hands its element to the JS module after each render, so
	// position and edge-flipping run client-side. Content stays server-owned.
	[Parameter]
	public bool Glued { get; set; }

	[Parameter]
	public Func<ElementReference, ValueTask>? GlueAsync { get; set; }

	protected ElementReference element;

	protected readonly RenderFragment _renderForeignObject;

	internal TooltipContext? context;

	public TooltipBase()
	{
		_renderForeignObject = RenderForeignObject;
	}

	protected abstract void RenderForeignObject(RenderTreeBuilder __builder);

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (Glued && context is not null && GlueAsync is not null)
		{
			await GlueAsync(element);
		}
	}

	public void Show(double x, double y, double offset, RenderFragment content, string? color = null, double? alpha = null)
	{
		var (px, py) = Coord.Project(x, y);

		// Null stays null: the theme falls back to --ggnet-tooltip-bg when the
		// geom passed no mark color.
		context = new(
		  X: px,
		  Y: py,
		  Offset: offset,
		  Content: content,
		  Color: color,
		  Opacity: alpha
		);

		_ = InvokeAsync(StateHasChanged);
	}

	// The mark's color/opacity ride as custom properties the theme derives
	// from; emitted on the tooltip host so both the classic bubble and the
	// top-layer popover inherit them.
	internal string? ContextStyle => context switch
	{
		null or { Color: null, Opacity: null } => null,
		{ Color: null, Opacity: { } opacity } => FormattableString.Invariant($"--tooltip-opacity: {opacity};"),
		{ Color: { } color, Opacity: null } => FormattableString.Invariant($"--tooltip-color: {color};"),
		{ Color: { } color, Opacity: { } opacity } => FormattableString.Invariant($"--tooltip-color: {color}; --tooltip-opacity: {opacity};"),
	};

	public void Hide()
	{
		context = null;

		_ = InvokeAsync(StateHasChanged);
	}
}
