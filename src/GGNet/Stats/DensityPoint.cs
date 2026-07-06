namespace GGNet;

public readonly record struct DensityPoint(double At, double Density);

public readonly record struct DensityPoint<TKey>(TKey Group, double At, double Density);
