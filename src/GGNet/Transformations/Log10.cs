namespace GGNet.Transformations;

public sealed class Log10 : ITransformation<double>
{
	public static readonly Log10 Instance = new();

	public double Apply(double value) => Math.Log10(value);

	public double Inverse(double value) => Math.Pow(10.0, value);
}