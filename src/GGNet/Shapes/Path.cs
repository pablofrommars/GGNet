using GGNet.Common;

namespace GGNet.Shapes;

public record Path : Shape
{
	public SortedBuffer<(double x, double y)> Points { get; }
		= new(comparer: Comparer.Instance);

	private sealed class Comparer : Comparer<(double x, double y)>
	{
		public static readonly Comparer Instance = new();

		public override int Compare([AllowNull] (double x, double y) a, [AllowNull] (double x, double y) b) => a.x.CompareTo(b.x);
	}

	public Elements.Line Aesthetic { get; set; } = default!;
}