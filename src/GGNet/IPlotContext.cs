namespace GGNet;

public interface IPlotContext
{
	Type PlotType { get; }

	Theme.Theme? Theme { get; set; }
}