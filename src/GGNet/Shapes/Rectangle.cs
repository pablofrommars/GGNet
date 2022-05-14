﻿namespace GGNet.Shapes;

public record Rectangle : Shape
{
	public double X { get; set; }

	public double Y { get; set; }

	public double Width { get; set; }

	public double Height { get; set; }

	public Elements.Rectangle Aesthetic { get; set; } = default!;
}