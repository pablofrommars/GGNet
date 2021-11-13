namespace GGNet.Elements;

public class Rectangle : IElement
{
	public Rectangle()
	{
		Fill = "inhenit";
		Alpha = 1.0;
		Color = "inhenit";
		Width = 0;

		Margin = new Margin();
	}

	public string Fill { get; set; }

	public double Alpha { get; set; }

	public string Color { get; set; }

	public double Width { get; set; }

	public Margin Margin { get; set; }
}