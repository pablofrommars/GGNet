namespace GGNet.Datasets;

public static class Tip
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
		public double Avg { get; set; }
		public double Lower { get; set; }
		public double Upper { get; set; }
	}

	public static Point[] Load() => new[]
	{
        new Point {Sex = Sex.Female, Day = Day.Thursday, Avg = 15.7562, Lower = 14.6905, Upper = 16.822},
        new Point {Sex = Sex.Female, Day = Day.Friday, Avg = 19.9333, Lower = 17.1944, Upper = 22.6723},
        new Point {Sex = Sex.Female, Day = Day.Saturday, Avg = 15.6464, Lower = 13.4124, Upper = 17.8804},
        new Point {Sex = Sex.Female, Day = Day.Sunday, Avg = 18.1556, Lower = 14.8491, Upper = 21.462},
        new Point {Sex = Sex.Male, Day = Day.Thursday, Avg = 16.5233, Lower = 14.8833, Upper = 18.1633},
        new Point {Sex = Sex.Male, Day = Day.Friday, Avg = 14.36, Lower = 12.1147, Upper = 16.6053},
        new Point {Sex = Sex.Male, Day = Day.Saturday, Avg = 15.1576, Lower = 13.9598, Upper = 16.3555},
        new Point {Sex = Sex.Male, Day = Day.Sunday, Avg = 16.2379, Lower = 13.9601, Upper = 18.5157}
    };
}
