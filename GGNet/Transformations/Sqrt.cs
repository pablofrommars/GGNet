namespace GGNet.Transformations;

public class Sqrt : ITransformation<double>
{
	public static readonly Sqrt Instance = new();

	public double Apply(double value) => Math.Sqrt(value);

	public double Inverse(double value) => value * value;
}