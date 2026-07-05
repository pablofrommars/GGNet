namespace GGNet;

public sealed class PolarOptions
{
  // SVG y grows downward, so -PI/2 points up: category 0 starts at 12 o'clock.
  public double StartAngle { get; set; } = -Math.PI / 2.0;

  public bool Clockwise { get; set; } = true;
}
