using GGNet.Common;

namespace GGNet.Shapes;

public record Area : Shape
{
	public SortedBuffer<(double x, double ymin, double ymax)> Points { get; }
		= new(comparer: Comparer.Instance);

	private sealed class Comparer : Comparer<(double x, double ymin, double ymax)>
	{
		public static readonly Comparer Instance = new();

		public override int Compare((double x, double ymin, double ymax) a, (double x, double ymin, double ymax) b)
            => a.x.CompareTo(b.x);
	}

	public Elements.Rectangle Aesthetic { get; set; } = default!;
}