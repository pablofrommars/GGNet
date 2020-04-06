using GGNet.Transformations;

namespace GGNet.Components
{
    public interface ICoord
    {
        double CoordX(double value);

        (double min, double max) XRange { get; }

        ITransformation<double> XTransformation { get; }

        double CoordY(double value);

        (double min, double max) YRange { get; }

        ITransformation<double> YTransformation { get; }
    }
}
