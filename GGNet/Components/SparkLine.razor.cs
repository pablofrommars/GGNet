using GGNet.Scales;
using GGNet.Transformations;

namespace GGNet.Components;

public partial class SparkLine<T, TX, TY> : PlotBase<T, TX, TY>, IPanel, ICoord
	where TX : struct
	where TY : struct
{
	[Parameter]
	public double Width { get; set; } = 150;

	[Parameter]
	public double Height { get; set; } = 50;

	private RenderChildPolicyBase renderChildPolicy = default!;

	private Zone Area;
	private Data<T, TX, TY>.Panel Panel = default!;
	private Position<TX> xscale = default!;
	private Position<TY> yscale = default!;

	protected Tooltips.SparkLine tooltip = default!;
	public ITooltip? Tooltip => tooltip;

	protected override void OnInitialized()
	{
		base.OnInitialized();

		renderChildPolicy = Policy.Child();

		Area.Width = Width;
		Area.Height = Height;

		Data.Init(false);

		Data.Render(true);

		Panel = Data.Panels[0];
		xscale = Panel.X;
		yscale = Panel.Y;

		Panel.Register(this);
	}

	public override void Render()
	{
		Data.Render(false);
		renderChildPolicy.Refresh();
	}

	public double CoordX(double value) => Area.X + xscale.Coord(value) * Area.Width;

	public (double min, double max) XRange => xscale.Range;

	public ITransformation<double> XTransformation => xscale.RangeTransformation;

	public double CoordY(double value) => Area.Y + (1 - yscale.Coord(value)) * Area.Height;

	public (double min, double max) YRange => yscale.Range;

	public ITransformation<double> YTransformation => yscale.RangeTransformation;
}