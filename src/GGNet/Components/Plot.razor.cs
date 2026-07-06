namespace GGNet.Components;

using Rendering;

using static Position;

public partial class Plot<T, TX, TY> : PlotBase<T, TX, TY>
  where TX : struct
  where TY : struct
{
	[Parameter]
	public double Width { get; init; } = 720;

	[Parameter]
	public double Height { get; init; } = 576;

	[Parameter]
	public string Theme { get; init; } = "default";

	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

	internal Zone Title;
	internal Zone SubTitle;

	internal Zone Legend;

	internal Zone Caption;

	private Zone wrapper;

	private IChildRenderModeHandler? definitionsRenderModeHandler;

	private bool loading;

	private readonly RenderFragment _renderLegendGradients;
	private readonly RenderFragment _renderLegend;
	private readonly RenderFragment _renderPanels;

	public Plot()
	{
		_renderLegendGradients = RenderLegendGradients;
		_renderLegend = RenderLegend;
		_renderPanels = RenderPanels;
	}

	private string? CssClass()
	{
		var @class = (string)(AdditionalAttributes?.GetValueOrDefault("class", string.Empty) ?? string.Empty);

		return (@class, loading) switch
		{
			("", true) => "ggnet loading",
			(_, true) => $"ggnet {@class} loading",
			("", false) => "ggnet",
			(_, false) => $"ggnet {@class}",
		};
	}

	protected override void OnInitialized()
	{
		base.OnInitialized();

		Context.Init();

		definitionsRenderModeHandler = RenderModeHandler?.Child();

		Compose(RenderTarget.All);
	}

	protected override async Task OnParametersSetAsync()
	{
		base.OnParametersSet();

		if (!Context.Initialized)
		{
			Context.Init();
			Context.Render();

			await RefreshAsync(RenderTarget.All, CancellationToken.None);
		}
	}

	// Runs the context pipeline and carves the plot-level zones. Child refresh
	// is the caller's concern: OnInitialized composes before children exist,
	// Render(target) composes and then refreshes them.
	private bool Compose(RenderTarget target)
	{
		if (target == RenderTarget.Loading)
		{
			loading = true;
			return false;
		}

		loading = false;

		Context.Render();

		var zones = Layout.PlotLayout.Compute(new(
			Width: Width,
			Height: Height,
			Title: Context.Title,
			SubTitle: Context.SubTitle,
			Caption: Context.Caption,
			HasLegends: Context.Legends.Count > 0,
			LegendsDimension: Context.Legends.Count > 0 ? Context.Legends.Dimension() : default,
			Style: Context.Style!));

		wrapper = zones.Wrapper;
		Title = zones.Title;
		SubTitle = zones.SubTitle;
		Legend = zones.Legend;
		Caption = zones.Caption;

		return true;
	}

	public override void Render(RenderTarget target)
	{
		if (!Compose(target))
		{
			return;
		}

		definitionsRenderModeHandler?.Refresh(target);

		for (var i = 0; i < Context.Panels.Count; i++)
		{
			Context.Panels[i].Component?.Refresh(target);
		}
	}
}
