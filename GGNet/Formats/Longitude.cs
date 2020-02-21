namespace GGNet.Formats
{
    public class Longitude : IFormatter<double>
    {
        public string Format(double value) => value >= 0 ? $"{value}\u00B0N" : $"{-value}\u00B0S";

        public static Longitude Instance => new Longitude();
    }
}
