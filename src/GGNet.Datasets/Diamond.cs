namespace GGNet.Datasets;

public static class Diamond
{
	public record Point
	{
		public double Carat { get; set; }
		public double Price { get; set; }
		public int Count { get; set; }
		public double Dx { get; set; }
		public double Dy { get; set; }
	}

	public static Point[] Load() => new[]
	{
        new Point { Carat = 0.2, Price = 326, Count = 14154, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.681, Price = 326, Count = 413, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.4405, Price = 1927.8872, Count = 10375, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.9215, Price = 1927.8872, Count = 3252, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.4025, Price = 1927.8872, Count = 9, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.681, Price = 3529.7744, Count = 5335, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.162, Price = 3529.7744, Count = 2292, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.643, Price = 3529.7744, Count = 26, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.4405, Price = 5131.6616, Count = 6, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.9215, Price = 5131.6616, Count = 5108, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.4025, Price = 5131.6616, Count = 1143, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.8835, Price = 5131.6616, Count = 18, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.3645, Price = 5131.6616, Count = 4, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.681, Price = 6733.5488, Count = 31, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.162, Price = 6733.5488, Count = 3006, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.643, Price = 6733.5488, Count = 412, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.124, Price = 6733.5488, Count = 43, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.605, Price = 6733.5488, Count = 2, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.086, Price = 6733.5488, Count = 1, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.9215, Price = 8335.4359, Count = 721, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.4025, Price = 8335.4359, Count = 1284, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.8835, Price = 8335.4359, Count = 99, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.3645, Price = 8335.4359, Count = 10, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.8455, Price = 8335.4359, Count = 4, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.681, Price = 9937.3231, Count = 1, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.162, Price = 9937.3231, Count = 722, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.643, Price = 9937.3231, Count = 895, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.124, Price = 9937.3231, Count = 68, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.605, Price = 9937.3231, Count = 7, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.086, Price = 9937.3231, Count = 5, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.9215, Price = 11539.2103, Count = 124, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.4025, Price = 11539.2103, Count = 758, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.8835, Price = 11539.2103, Count = 310, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.3645, Price = 11539.2103, Count = 34, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.8455, Price = 11539.2103, Count = 3, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.3265, Price = 11539.2103, Count = 1, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.8075, Price = 11539.2103, Count = 1, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.162, Price = 13141.0975, Count = 116, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.643, Price = 13141.0975, Count = 550, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.124, Price = 13141.0975, Count = 413, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.605, Price = 13141.0975, Count = 10, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.086, Price = 13141.0975, Count = 2, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.567, Price = 13141.0975, Count = 1, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.9215, Price = 14742.9847, Count = 16, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.4025, Price = 14742.9847, Count = 298, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.8835, Price = 14742.9847, Count = 335, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.3645, Price = 14742.9847, Count = 120, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.8455, Price = 14742.9847, Count = 6, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.8075, Price = 14742.9847, Count = 2, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.162, Price = 16344.8719, Count = 24, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.643, Price = 16344.8719, Count = 214, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.124, Price = 16344.8719, Count = 473, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.605, Price = 16344.8719, Count = 57, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.086, Price = 16344.8719, Count = 4, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.567, Price = 16344.8719, Count = 2, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 4.048, Price = 16344.8719, Count = 1, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 0.9215, Price = 17946.7591, Count = 9, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.4025, Price = 17946.7591, Count = 56, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.8835, Price = 17946.7591, Count = 337, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.3645, Price = 17946.7591, Count = 156, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.8455, Price = 17946.7591, Count = 13, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 4.2885, Price = 17946.7591, Count = 2, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 5.2505, Price = 17946.7591, Count = 1, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 1.643, Price = 19548.6463, Count = 7, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.124, Price = 19548.6463, Count = 32, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 2.605, Price = 19548.6463, Count = 3, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.086, Price = 19548.6463, Count = 2, Dx = 0.4810, Dy = 1601.887 },
        new Point { Carat = 3.567, Price = 19548.6463, Count = 1, Dx = 0.4810, Dy = 1601.887 }
    };
}
