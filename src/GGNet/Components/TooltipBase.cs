namespace GGNet.Components;

public abstract class TooltipBase : ComponentBase, ITooltip
{
	[Parameter]
	public required ICoord Coord { get; set; }

	[Parameter]
	public Zone Zone { get; set; }

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
