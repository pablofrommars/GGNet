namespace GGNet.Elements
{
    public class Rectangle : IElement
    {
        public Rectangle()
        {
            Margin = new Margin();
        }

        public string Fill { get; set; }

        public double Alpha { get; set; }

        public Margin Margin { get; set; }
    }
}
