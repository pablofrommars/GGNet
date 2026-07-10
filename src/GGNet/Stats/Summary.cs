namespace GGNet;

public readonly record struct Summary(double X, double Center, double Lower, double Upper);

public readonly record struct Summary<TKey>(TKey Group, double X, double Center, double Lower, double Upper);
