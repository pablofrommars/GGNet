namespace GGNet
{
    using static Units;

    public struct Size
    {
        public Size(double value, Units units = em)
        {
            Value = value;
            Units = units;
        }

        public double Value { get; set; }

        public Units Units { get; set; }

        public override string ToString() => $"{Value}{Units}";
    }
}
