namespace GGNet;

public interface IPlotContext
{
	Type PlotType { get; }

	Style? Style { get; set; }
}
