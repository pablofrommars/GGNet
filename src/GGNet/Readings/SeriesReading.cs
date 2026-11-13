namespace GGNet.Readings;

internal readonly record struct SeriesReading(string Series, string? Group, string Name, ReadingKind Kind, double? Sample, string Value, int Row);
