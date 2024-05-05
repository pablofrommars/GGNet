using GGNet.Common;

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

  [Parameter]
  public ObjectPool<StringBuilder>? StringBuilderPool { get; init; }

  public Zone Title;
  public Zone SubTitle;

  public Zone Legend;

  public Zone Caption;

  private Zone wrapper;

  private IChildRenderPolicy? definitionsPolicy;

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

  public string? PlotClass() => loading
    ? $"ggnet loading"
    : "ggnet";

  protected override void OnInitialized()
  {
    base.OnInitialized();

    Context.Init();

    definitionsPolicy = Policy?.Child();

    Render(true, RenderTarget.All);
  }

  protected override async Task OnParametersSetAsync()
  {
    base.OnParametersSet();

    if (!Context.Initialized)
    {
      Context.Init();
      Context.Render(true);

      await RefreshAsync(RenderTarget.All);
    }
  }

  protected void Render(bool firstRender, RenderTarget target)
  {
    if (target == RenderTarget.Loading)
    {
      loading = true;
      return;
    }

    loading = false;

    Context.Render(firstRender);

    wrapper.X = 0;
    wrapper.Y = 0;
    wrapper.Width = Width;
    wrapper.Height = Height;

    if (!string.IsNullOrEmpty(Context.Title))
    {
      var width = Context.Title.Height(Context.Style!.Plot.Title.FontSize);
      var height = Context.Title.Height(Context.Style!.Plot.Title.FontSize);

      Title.X = Context.Style!.Plot.Title.Margin.Left;
      Title.Y = Context.Style!.Plot.Title.Margin.Top + height;
      Title.Width = Context.Style!.Plot.Title.Margin.Left + width + Context.Style.Plot.Title.Margin.Right;
      Title.Height = Context.Style!.Plot.Title.Margin.Top + height + Context.Style.Plot.Title.Margin.Bottom;

      wrapper.Y += Title.Height;
      wrapper.Height -= Title.Height;
    }

    if (!string.IsNullOrEmpty(Context.SubTitle))
    {
      var width = Context.SubTitle.Height(Context.Style!.Plot.SubTitle.FontSize);
      var height = Context.SubTitle.Height(Context.Style!.Plot.SubTitle.FontSize);

      SubTitle.X = Context.Style!.Plot.SubTitle.Margin.Left;
      SubTitle.Y = Title.Height + Context.Style!.Plot.SubTitle.Margin.Top + height;
      SubTitle.Width = Context.Style!.Plot.SubTitle.Margin.Left + width + Context.Style!.Plot.SubTitle.Margin.Right;
      SubTitle.Height = Context.Style!.Plot.SubTitle.Margin.Top + height + Context.Style!.Plot.SubTitle.Margin.Bottom;

      wrapper.Y += SubTitle.Height;
      wrapper.Height -= SubTitle.Height;
    }

    if (!string.IsNullOrEmpty(Context.Caption))
    {
      var width = Context.Caption.Height(Context.Style!.Plot.Caption.FontSize);
      var height = Context.Caption.Height(Context.Style!.Plot.Caption.FontSize);

      Caption.Y = Height - Context.Style!.Plot.Caption.Margin.Bottom;
      Caption.Width = Context.Style!.Plot.Caption.Margin.Left + width + Context.Style!.Plot.Caption.Margin.Right;
      Caption.Height = Context.Style!.Plot.Caption.Margin.Top + height + Context.Style!.Plot.Caption.Margin.Bottom;

      wrapper.Height -= Caption.Height;
    }

    if (Context.Legends.Count > 0)
    {
      var (width, height) = Context.Legends.Dimension();

      if (width > 0 && height > 0)
      {
        if (Context.Style!.Legend.Position == Right)
        {
          Legend.X = Width - width - Context.Style!.Legend.Margin.Right;
          Legend.Y = wrapper.Y + (wrapper.Height - height) / 2.0;
          Legend.Width = Context.Style!.Legend.Margin.Left + width + Context.Style!.Legend.Margin.Right;
          Legend.Height = wrapper.Height;

          wrapper.Width -= Legend.Width;
        }
        else if (Context.Style!.Legend.Position == Left)
        {
          Legend.X = Context.Style!.Legend.Margin.Left;
          Legend.Y = wrapper.Y + (wrapper.Height - height) / 2.0; ;
          Legend.Width = Context.Style!.Legend.Margin.Left + width + Context.Style!.Legend.Margin.Right;
          Legend.Height = wrapper.Height;

          wrapper.X += Legend.Width;
          wrapper.Width -= Legend.Width;
        }
        else if (Context.Style!.Legend.Position == Top)
        {
          Legend.X = wrapper.X + (wrapper.Width - width) / 2.0;
          Legend.Y = wrapper.Y + Context.Style!.Legend.Margin.Top;
          Legend.Width = wrapper.Width;
          Legend.Height = Context.Style!.Legend.Margin.Top + height + Context.Style!.Legend.Margin.Bottom;

          wrapper.Y += Legend.Height;
          wrapper.Height -= Legend.Height;
        }
        else if (Context.Style!.Legend.Position == Bottom)
        {
          Legend.X = wrapper.X + (wrapper.Width - width) / 2.0;
          Legend.Y = wrapper.Y + wrapper.Height - height - Context.Style!.Legend.Margin.Bottom;
          Legend.Width = wrapper.Width;
          Legend.Height = Context.Style!.Legend.Margin.Top + height + Context.Style!.Legend.Margin.Bottom;

          wrapper.Height -= Legend.Height;
        }
      }
    }

    if (Caption.Width > 0)
    {
      Caption.X = wrapper.X + wrapper.Width - Context.Style!.Plot.Caption.Margin.Right;
    }

    if (!firstRender)
    {
      definitionsPolicy?.Refresh(target);

      for (var i = 0; i < Context.Panels.Count; i++)
      {
        Context.Panels[i].Component?.Refresh(target);
      }
    }
  }

  public override void Render(RenderTarget target) => Render(false, target);
}
