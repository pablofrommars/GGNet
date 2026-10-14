namespace GGNet.Coords;

internal static class Polar
{
	public static (double x, double y) Project(
	  double angular, double radial,
	  double centerX, double centerY, double radius,
	  double startAngle, bool clockwise)
	{
		var theta = Angle(angular, startAngle, clockwise);
		var r = radial * radius;

		return (centerX + r * Math.Cos(theta), centerY + r * Math.Sin(theta));
	}

	public static double Angle(double angular, double startAngle, bool clockwise)
	  => startAngle + (clockwise ? 1.0 : -1.0) * 2.0 * Math.PI * angular;
}
