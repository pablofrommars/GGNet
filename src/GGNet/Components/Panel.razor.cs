using GGNet.Scales;
using GGNet.Transformations;

using static System.Math;

using static GGNet.Position;

namespace GGNet.Components;

using Rendering;

public partial class Panel<T, TX, TY> : ComponentBase, ICoord, IPanel
  where TX : struct
  where TY : struct
{
  [CascadingParameter]
  public required Plot<T, TX, TY> Plot { get; init; }

  [Parameter]
  public required Data.Panel<T, TX, TY> Data { get; init; }

  [Parameter]
  public double X { get; set; }

  [Parameter]
  public double Y { get; set; }

  [Parameter]
  public double Width { get; set; }

  [Parameter]
  public double Height { get; set; }

  [Parameter]
  public bool First { get; set; }

  [Parameter]
  public bool Last { get; set; }

  private IChildRenderPolicy? policy;
  private IChildRenderPolicy? areaPolicy;

  private Position<TX> xscale = default!;
  private Position<TY> yscale = default!;

  private Zone xStrip;
  private Zone yStrip;

  public Zone yAxisText;
  private Zone yAxisTitle;

  private Zone xAxisText;
  private Zone xAxisTitle;

  public Zone Area;

  protected Tooltips.Plot tooltip = default!;
  public ITooltip? Tooltip => tooltip;

  protected string clip = default!;

  protected bool firstRender = true;

  private readonly RenderFragment _renderTitle;
  private readonly RenderFragment _renderSubTitle;
  private readonly RenderFragment _renderStrip;
  private readonly RenderFragment _renderGrid;
  private readonly RenderFragment _renderCaption;
  private readonly RenderFragment _renderXLab;
  private readonly RenderFragment _renderYLab;

  public Panel()
  {
    _renderTitle = RenderTitle;
    _renderSubTitle = RenderSubTitle;
    _renderStrip = RenderStrip;
    _renderGrid = RenderGrid;
    _renderCaption = RenderCaption;
    _renderXLab = RenderXLab;
    _renderYLab = RenderYLab;
  }

  protected override void OnInitialized()
  {
    policy = Plot.Policy?.Child();
    areaPolicy = Plot.Policy?.Child();

    Data.Register(this);

    clip = Plot.Id + "-" + Data.Id;

    xscale = Data.X;
    yscale = Data.Y;
  }

  protected override void OnParametersSet()
  {
    if (Data.Registered)
    {
      return;
    }

    Data.Register(this);

    clip = Plot.Id + "-" + Data.Id;

    xscale = Data.X;
    yscale = Data.Y;

    Refresh(RenderTarget.All);
  }

  protected void Render(bool firstRender)
  {
    Area.X = X;
    Area.Y = Y;
    Area.Width = Width;
    Area.Height = Height;

    if (!string.IsNullOrEmpty(Data.Strip.x))
    {
      var width = Data.Strip.x.Width(Data.Data.Style!.Strip.Text.X.FontSize);
      var height = Data.Strip.x.Height(Data.Data.Style!.Strip.Text.X.FontSize);

      //xStrip.X = X + Data.Data.Theme.Strip.Text.X.Margin.Left;
      xStrip.Y = Y + Data.Data.Style!.Strip.Text.X.Margin.Top + height;
      xStrip.Width = Data.Data.Style!.Strip.Text.X.Margin.Left + width + Data.Data.Style!.Strip.Text.X.Margin.Right;
      xStrip.Height = Data.Data.Style!.Strip.Text.X.Margin.Top + height + Data.Data.Style!.Strip.Text.X.Margin.Bottom;

      Area.Y += xStrip.Height;
      Area.Height -= xStrip.Height;
    }

    if (!string.IsNullOrEmpty(Data.Strip.y))
    {
      var width = Data.Strip.y.Height(Data.Data.Style!.Strip.Text.Y.FontSize);
      var height = Data.Strip.y.Width(Data.Data.Style!.Strip.Text.Y.FontSize);

      yStrip.X = Area.X + Area.Width - Data.Data.Style!.Strip.Text.X.Margin.Right - width;
      yStrip.Y = Area.Y + Data.Data.Style!.Strip.Text.Y.Margin.Top;
      yStrip.Width = Data.Data.Style!.Strip.Text.Y.Margin.Left + width + Data.Data.Style!.Strip.Text.Y.Margin.Right;
      yStrip.Height = Data.Data.Style!.Strip.Text.Y.Margin.Top + height + Data.Data.Style!.Strip.Text.Y.Margin.Bottom;

      Area.Width -= yStrip.Width;
    }

    if (Data.Axis.y)
    {
      var axisWidth = Data.Data.Axis.width;

      if (Data.Data.Style!.Axis.Y == Left)
      {
        if (Data.YLab.width > 0.0 || Data.Y.Titles.Any())
        {
          yAxisTitle.X = Area.X + Data.Data.Style.Axis.Title.Y.Margin.Left + Data.YLab.width;
          yAxisTitle.Y = Area.Y + Data.Data.Style.Axis.Title.Y.Margin.Bottom;
          yAxisTitle.Width = Data.Data.Style.Axis.Title.Y.Margin.Left + Data.YLab.width + Data.Data.Style.Axis.Title.Y.Margin.Right;
          yAxisTitle.Height = Area.Height;

          Area.X += yAxisTitle.Width;
          Area.Width -= yAxisTitle.Width;
        }

        yAxisText.X = Area.X + Data.Data.Style.Axis.Text.Y.Margin.Left + axisWidth;
        yAxisText.Y = Area.Y;
        yAxisText.Width = Data.Data.Style.Axis.Text.Y.Margin.Left + axisWidth + Data.Data.Style.Axis.Text.Y.Margin.Right;

        Area.X += yAxisText.Width;
        Area.Width -= yAxisText.Width;
      }
      else if (Data.Data.Style.Axis.Y == Right)
      {
        if (Data.YLab.width > 0.0 || Data.Y.Titles.Any())
        {
          yAxisTitle.X = Area.X + Area.Width - Data.YLab.width - Data.Data.Style.Axis.Title.Y.Margin.Right;
          yAxisTitle.Y = Area.Y + Data.Data.Style.Axis.Title.Y.Margin.Bottom;
          yAxisTitle.Width = Data.Data.Style.Axis.Title.Y.Margin.Left + Data.YLab.width + Data.Data.Style.Axis.Title.Y.Margin.Right;
          yAxisTitle.Height = Area.Height;

          Area.Width -= yAxisTitle.Width;
        }

        yAxisText.X = Area.X + Area.Width - axisWidth;
        yAxisText.Y = Area.Y;
        yAxisText.Width = Data.Data.Style.Axis.Text.Y.Margin.Left + axisWidth + Data.Data.Style.Axis.Text.Y.Margin.Right;

        Area.Width -= yAxisText.Width;
      }
    }

    if (Data.Axis.x)
    {
      var xTitlesHeight = Data.XLab.height;

      if (Data.X.Titles.Any())
      {
        xTitlesHeight = Max(xTitlesHeight, Data.Data.Style!.Axis.Title.X.FontSize.Height());
      }

      if (xTitlesHeight > 0.0)
      {
        xAxisTitle.X = Area.X + Area.Width - Data.Data.Style!.Axis.Title.X.Margin.Right;
        xAxisTitle.Y = Area.Y + Area.Height - Data.Data.Style!.Axis.Title.X.Margin.Bottom;
        xAxisTitle.Width = Data.Data.Style.Axis.Title.X.Margin.Left + Area.Width + Data.Data.Style!.Axis.Title.X.Margin.Right;
        xAxisTitle.Height = Data.Data.Style.Axis.Title.X.Margin.Top + xTitlesHeight + Data.Data.Style!.Axis.Title.X.Margin.Bottom;

        Area.Height -= xAxisTitle.Height;
      }

      var axisHeight = Data.Data.Axis.height;

      xAxisText.X = Area.X;
      xAxisText.Y = Area.Y + Area.Height - Data.Data.Style!.Axis.Text.X.Margin.Bottom;
      xAxisText.Width = Area.Width;
      xAxisText.Height = Data.Data.Style.Axis.Text.X.Margin.Top + axisHeight + Data.Data.Style.Axis.Text.X.Margin.Bottom;

      Area.Height -= xAxisText.Height;
    }

    if (xStrip.Width > 0)
    {
      xStrip.X = Area.X + Data.Data.Style!.Strip.Text.X.Margin.Left;
    }

    if (yAxisText.Width > 0)
    {
      yAxisText.Height = Area.Height;
    }

    if (!firstRender)
    {
      areaPolicy?.Refresh(RenderTarget.Data);
    }
  }

  public void Refresh(RenderTarget target) => policy?.Refresh(target);

  protected override bool ShouldRender() => policy?.ShouldRender() ?? true;

  public double ToX(double value) => Area.X + xscale.Coord(value) * Area.Width;

  public (double min, double max) XRange => xscale.Range;

  public ITransformation<double> XTransformation => xscale.RangeTransformation;

  public double ToY(double value) => Area.Y + (1 - yscale.Coord(value)) * Area.Height;

  public (double min, double max) YRange => yscale.Range;

  public ITransformation<double> YTransformation => yscale.RangeTransformation;
}
