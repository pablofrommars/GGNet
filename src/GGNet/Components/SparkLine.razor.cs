using GGNet.Buffers;
using GGNet.Transformations;

namespace GGNet.Components;

using Rendering;

public partial class SparkLine<T, TX, TY> : PlotBase<T, TX, TY>, IPanel, ICoord
	where TX : struct
	where TY : struct
{
	[Parameter]
	public double Width { get; set; } = 150;

	[Parameter]
	public double Height { get; set; } = 50;

  private IChildRenderPolicy? renderChildPolicy;
  private IChildRenderPolicy? definitionsPolicy;

	private Zone Area;
	private Data.Panel<T, TX, TY> Panel = default!;
	private Scales.Position<TX> xscale = default!;
	private Scales.Position<TY> yscale = default!;

	protected Tooltips.SparkLine tooltip = default!;
	public ITooltip? Tooltip => tooltip;

	protected override void OnInitialized()
	{
		base.OnInitialized();

		renderChildPolicy = Policy?.Child();
		definitionsPolicy = Policy?.Child();

		Area.Width = Width;
		Area.Height = Height;

		Context.Init(false);

		Context.Render(true);

		Panel = Context.Panels[0];
		xscale = Panel.X;
		yscale = Panel.Y;

		Panel.Register(this);
	}

	public override void Render(RenderTarget target)
	{
		Context.Render(false);
		definitionsPolicy?.Refresh(target);
		renderChildPolicy?.Refresh(target);
	}

	public double ToX(double value) => Area.X + xscale.Coord(value) * Area.Width;

	public (double min, double max) XRange => xscale.Range;

	public ITransformation<double> XTransformation => xscale.RangeTransformation;

	public double ToY(double value) => Area.Y + (1 - yscale.Coord(value)) * Area.Height;

	public (double min, double max) YRange => yscale.Range;

	public ITransformation<double> YTransformation => yscale.RangeTransformation;
}
