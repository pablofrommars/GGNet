﻿namespace GGNet;

public static class SVGUtils
{
	public static string Id(object obj) => "gg" + Convert.ToBase64String(BitConverter.GetBytes(obj.GetHashCode()))[0..^2]
		.Replace('+', '-')
		.Replace('/', '_');
}