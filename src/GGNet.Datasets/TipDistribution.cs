﻿namespace GGNet.Datasets;

public static class TipDistribution
{
	public enum Sex
	{
		Female,
		Male
	}

	public enum Day
	{
		Thursday,
		Friday,
		Saturday,
		Sunday
	}

	public record Point
	{
		public Sex Sex { get; set; }
		public Day Day { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
	}

	public static Point[] Load() => new[]
	{
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 5.9, Y = 0.1178 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 7.0548, Y = 0.101 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 8.2097, Y = 0.0713 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 9.3645, Y = 0.0718 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 10.5194, Y = 0.1359 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 11.6742, Y = 0.2814 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 12.829, Y = 0.5071 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 13.9839, Y = 0.7653 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 15.1387, Y = 0.9551 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 16.2935, Y = 1 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 17.4484, Y = 0.9335 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 18.6032, Y = 0.8423 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 19.7581, Y = 0.7391 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 20.9129, Y = 0.5759 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 22.0677, Y = 0.3732 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 23.2226, Y = 0.221 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 24.3774, Y = 0.1552 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 25.5323, Y = 0.1282 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 26.6871, Y = 0.0924 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 27.8419, Y = 0.0492 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 28.9968, Y = 0.0186 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 30.1516, Y = 0.0049 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 31.3065, Y = 0.0009 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 32.4613, Y = 0.0001 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 33.6161, Y = 0 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 34.771, Y = 0.0002 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 35.9258, Y = 0.0013 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 37.0806, Y = 0.0066 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 38.2355, Y = 0.0232 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 39.3903, Y = 0.0571 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 40.5452, Y = 0.0979 },
        new Point { Sex = Sex.Female, Day = Day.Sunday, X = 41.7, Y = 0.1173 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 6.6, Y = 0.3624 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 8.6774, Y = 0.6602 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 10.7548, Y = 0.8397 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 12.8323, Y = 0.8959 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 14.9097, Y = 1 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 16.9871, Y = 0.9986 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 19.0645, Y = 0.7545 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 21.1419, Y = 0.5225 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 23.2194, Y = 0.3987 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 25.2968, Y = 0.2454 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 27.3742, Y = 0.1126 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 29.4516, Y = 0.0483 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 31.529, Y = 0.0154 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 33.6065, Y = 0.0026 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 35.6839, Y = 0.0002 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 37.7613, Y = 6.5347e-006 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 39.8387, Y = 9.7821e-008 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 41.9161, Y = 6.5413e-010 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 43.9935, Y = 1.9313e-012 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 46.071, Y = 2.5065e-015 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 48.1484, Y = 4.1758e-018 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 50.2258, Y = 5.0288e-017 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 52.3032, Y = 2.015e-016 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 54.3806, Y = 2.0518e-013 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 56.4581, Y = 9.3522e-011 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 58.5355, Y = 1.872e-008 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 60.6129, Y = 1.6533e-006 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 62.6903, Y = 0.0001 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 64.7677, Y = 0.0011 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 66.8452, Y = 0.0087 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 68.9226, Y = 0.0295 },
        new Point { Sex = Sex.Male, Day = Day.Sunday, X = 71, Y = 0.0443 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 3.6, Y = 0.071 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 4.4258, Y = 0.1015 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 5.2516, Y = 0.1503 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 6.0774, Y = 0.2212 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 6.9032, Y = 0.3049 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 7.729, Y = 0.3828 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 8.5548, Y = 0.4404 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 9.3806, Y = 0.4808 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 10.2065, Y = 0.5235 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 11.0323, Y = 0.5901 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 11.8581, Y = 0.6878 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 12.6839, Y = 0.8035 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 13.5097, Y = 0.9108 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 14.3355, Y = 0.9816 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 15.1613, Y = 1 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 15.9871, Y = 0.9708 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 16.8129, Y = 0.9204 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 17.6387, Y = 0.8783 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 18.4645, Y = 0.8517 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 19.2903, Y = 0.8168 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 20.1161, Y = 0.7402 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 20.9419, Y = 0.6108 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 21.7677, Y = 0.4502 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 22.5935, Y = 0.2951 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 23.4194, Y = 0.173 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 24.2452, Y = 0.0921 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 25.071, Y = 0.0469 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 25.8968, Y = 0.0275 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 26.7226, Y = 0.0253 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 27.5484, Y = 0.0323 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 28.3742, Y = 0.0409 },
        new Point { Sex = Sex.Male, Day = Day.Saturday, X = 29.2, Y = 0.0446 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 5.6, Y = 0.2804 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 6.471, Y = 0.3237 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 7.3419, Y = 0.35 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 8.2129, Y = 0.3714 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 9.0839, Y = 0.4067 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 9.9548, Y = 0.4729 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 10.8258, Y = 0.5763 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 11.6968, Y = 0.7073 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 12.5677, Y = 0.8413 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 13.4387, Y = 0.9468 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 14.3097, Y = 1 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 15.1806, Y = 0.9983 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 16.0516, Y = 0.9608 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 16.9226, Y = 0.9113 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 17.7935, Y = 0.8583 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 18.6645, Y = 0.7897 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 19.5355, Y = 0.6901 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 20.4065, Y = 0.5605 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 21.2774, Y = 0.4217 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 22.1484, Y = 0.2992 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 23.0194, Y = 0.2071 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 23.8903, Y = 0.1462 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 24.7613, Y = 0.111 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 25.6323, Y = 0.0961 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 26.5032, Y = 0.095 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 27.3742, Y = 0.0996 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 28.2452, Y = 0.102 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 29.1161, Y = 0.1 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 29.9871, Y = 0.0972 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 30.8581, Y = 0.0973 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 31.729, Y = 0.0988 },
        new Point { Sex = Sex.Female, Day = Day.Saturday, X = 32.6, Y = 0.0958 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 7.9, Y = 0.2539 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 8.5032, Y = 0.3065 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 9.1065, Y = 0.3625 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 9.7097, Y = 0.4256 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 10.3129, Y = 0.4993 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 10.9161, Y = 0.585 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 11.5194, Y = 0.68 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 12.1226, Y = 0.7769 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 12.7258, Y = 0.8655 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 13.329, Y = 0.936 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 13.9323, Y = 0.9814 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 14.5355, Y = 1 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 15.1387, Y = 0.9953 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 15.7419, Y = 0.9746 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 16.3452, Y = 0.9474 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 16.9484, Y = 0.9213 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 17.5516, Y = 0.9 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 18.1548, Y = 0.8821 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 18.7581, Y = 0.8619 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 19.3613, Y = 0.8327 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 19.9645, Y = 0.7893 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 20.5677, Y = 0.7312 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 21.171, Y = 0.6617 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 21.7742, Y = 0.5871 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 22.3774, Y = 0.5136 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 22.9806, Y = 0.4458 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 23.5839, Y = 0.3855 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 24.1871, Y = 0.3324 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 24.7903, Y = 0.2853 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 25.3935, Y = 0.2425 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 25.9968, Y = 0.2031 },
        new Point { Sex = Sex.Male, Day = Day.Thursday, X = 26.6, Y = 0.1663 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 7.3, Y = 0.0669 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 7.7484, Y = 0.0646 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 8.1968, Y = 0.0579 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 8.6452, Y = 0.0512 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 9.0935, Y = 0.05 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 9.5419, Y = 0.0597 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 9.9903, Y = 0.0848 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 10.4387, Y = 0.1281 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 10.8871, Y = 0.1915 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 11.3355, Y = 0.2754 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 11.7839, Y = 0.3797 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 12.2323, Y = 0.5024 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 12.6806, Y = 0.6373 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 13.129, Y = 0.7723 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 13.5774, Y = 0.8895 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 14.0258, Y = 0.97 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 14.4742, Y = 1 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 14.9226, Y = 0.9771 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 15.371, Y = 0.9114 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 15.8194, Y = 0.8222 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 16.2677, Y = 0.7312 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 16.7161, Y = 0.6559 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 17.1645, Y = 0.6044 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 17.6129, Y = 0.5758 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 18.0613, Y = 0.5622 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 18.5097, Y = 0.5539 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 18.9581, Y = 0.5426 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 19.4065, Y = 0.5223 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 19.8548, Y = 0.4895 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 20.3032, Y = 0.4424 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 20.7516, Y = 0.3819 },
        new Point { Sex = Sex.Female, Day = Day.Thursday, X = 21.2, Y = 0.3119 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 10.4, Y = 0.6457 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 10.7871, Y = 0.7673 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 11.1742, Y = 0.8718 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 11.5613, Y = 0.9484 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 11.9484, Y = 0.9912 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 12.3355, Y = 1 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 12.7226, Y = 0.9807 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 13.1097, Y = 0.9424 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 13.4968, Y = 0.8948 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 13.8839, Y = 0.8443 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 14.271, Y = 0.7932 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 14.6581, Y = 0.7404 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 15.0452, Y = 0.6839 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 15.4323, Y = 0.6225 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 15.8194, Y = 0.5579 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 16.2065, Y = 0.4938 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 16.5935, Y = 0.4344 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 16.9806, Y = 0.3826 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 17.3677, Y = 0.3391 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 17.7548, Y = 0.3018 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 18.1419, Y = 0.2681 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 18.529, Y = 0.2359 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 18.9161, Y = 0.205 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 19.3032, Y = 0.1772 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 19.6903, Y = 0.1557 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 20.0774, Y = 0.1437 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 20.4645, Y = 0.1428 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 20.8516, Y = 0.1519 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 21.2387, Y = 0.1676 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 21.6258, Y = 0.1845 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 22.0129, Y = 0.1968 },
        new Point { Sex = Sex.Male, Day = Day.Friday, X = 22.4, Y = 0.2 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 14.3, Y = 0.5461 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 14.6871, Y = 0.6022 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 15.0742, Y = 0.6535 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 15.4613, Y = 0.7004 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 15.8484, Y = 0.7441 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 16.2355, Y = 0.7862 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 16.6226, Y = 0.8276 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 17.0097, Y = 0.8686 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 17.3968, Y = 0.9083 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 17.7839, Y = 0.9444 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 18.171, Y = 0.974 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 18.5581, Y = 0.9936 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 18.9452, Y = 1 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 19.3323, Y = 0.991 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 19.7194, Y = 0.9659 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 20.1065, Y = 0.9254 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 20.4935, Y = 0.8719 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 20.8806, Y = 0.8092 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 21.2677, Y = 0.7418 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 21.6548, Y = 0.6744 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 22.0419, Y = 0.6118 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 22.429, Y = 0.5579 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 22.8161, Y = 0.5159 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 23.2032, Y = 0.4875 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 23.5903, Y = 0.473 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 23.9774, Y = 0.471 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 24.3645, Y = 0.4786 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 24.7516, Y = 0.4913 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 25.1387, Y = 0.5043 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 25.5258, Y = 0.5122 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 25.9129, Y = 0.5108 },
        new Point { Sex = Sex.Female, Day = Day.Friday, X = 26.3, Y = 0.4972 }
    };
}