using GGNet.Transformations;

namespace GGNet.Components;

public interface ICoord
{
	double ToX(double value);

	(double min, double max) XRange { get; }

	ITransformation<double> XTransformation { get; }

	double ToY(double value);

	(double min, double max) YRange { get; }

	ITransformation<double> YTransformation { get; }
}
