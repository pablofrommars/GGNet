namespace GGNet.Formats
{
    public class Latitude : IFormatter<double>
    {
        public string Format(double value) => value <= 0 ? $"{-value}\u00B0W" : $"{value}\u00B0E";

        public static Latitude Instance => new Latitude();
    }
}
