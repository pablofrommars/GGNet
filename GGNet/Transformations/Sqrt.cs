using System;

namespace GGNet.Transformations
{
    public class Sqrt : ITransformation<double>
    {
        public double Apply(double value) => Math.Sqrt(value);

        public double Inverse(double value) => value * value;

        public static Sqrt Instance = new Sqrt();
    }
}
