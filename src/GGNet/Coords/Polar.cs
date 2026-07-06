namespace GGNet.Coords;

internal static class Polar
{
	// cx, cy are normalized [0,1] scale fractions (angular, radial).
	public static (double x, double y) Project(
	  double cx, double cy,
	  double centerX, double centerY, double radius,
	  double startAngle, bool clockwise)
	{
		var theta = Angle(cx, startAngle, clockwise);
		var r = cy * radius;

		return (centerX + r * Math.Cos(theta), centerY + r * Math.Sin(theta));
	}

	public static double Angle(double cx, double startAngle, bool clockwise)
	  => startAngle + (clockwise ? 1.0 : -1.0) * 2.0 * Math.PI * cx;
}
