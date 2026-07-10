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

	(double x, double y) Project(double x, double y) => (ToX(x), ToY(y));

	(double x, double y) Unproject(double px, double py);
}
