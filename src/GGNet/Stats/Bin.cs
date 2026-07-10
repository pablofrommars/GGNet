namespace GGNet;

public readonly record struct Bin(double Min, double Mid, double Max, int Count, double Density);

public readonly record struct Bin<TKey>(TKey Group, double Min, double Mid, double Max, int Count, double Density);
