namespace GGNet.Readings;

internal interface ISeriesIndex
{
	string Name { get; }

	int Count { get; }

	double Coordinate(int row);

	(double x, double y) Position(int row);

	SeriesReading Read(double cursor);

	double? After(double cursor);

	double? Before(double cursor);
}
