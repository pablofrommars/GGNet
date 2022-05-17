namespace GGNet;

public interface IData
{
	Type PlotType { get; }

	Theme.Theme? Theme { get; set; }
}