namespace GGNet
{
    public struct Size
    {
        public double Value { get; set; }

        public Units Units { get; set; }

        public override string ToString() => $"{Value}{Units}";
    }
}
