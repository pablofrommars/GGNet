using GGNet.Common;

namespace GGNet.Shapes;

public readonly record struct Path : IShape
{
  public Path() { }

  public string? Classes { get; init; }

  public Func<MouseEventArgs, Task>? OnClick { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOver { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOut { get; init; }

  public required Elements.Line Aesthetic { get; init; }

	public SortedBuffer<(double x, double y)> Points { get; }
		= new(comparer: Comparer.Instance);

	private sealed class Comparer : Comparer<(double x, double y)>
	{
		public static readonly Comparer Instance = new();

		public override int Compare([AllowNull] (double x, double y) a, [AllowNull] (double x, double y) b) => a.x.CompareTo(b.x);
	}
}
