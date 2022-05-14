﻿namespace GGNet.Formats;

public class Standard<T> : IFormatter<T>
	where T : notnull
{
	public static Standard<T> Instance => new();

	public string Format(T value) => value.ToString()!;
}