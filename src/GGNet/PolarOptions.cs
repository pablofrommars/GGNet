namespace GGNet;

public sealed class PolarOptions
{
	/// <summary>Angle of category 0, in radians. SVG y grows downward, so the default -π/2 starts at 12 o'clock.</summary>
	public double StartAngle { get; set; } = -Math.PI / 2.0;

	public bool Clockwise { get; set; } = true;
}
