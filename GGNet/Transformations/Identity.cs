﻿namespace GGNet.Transformations;

public class Identity<T> : ITransformation<T>
{
	public static Identity<T> Instance = new();

	public T Apply(T value) => value;

	public T Inverse(T value) => value;
}