using GGNet.Common;

namespace GGNet.Shapes;

public readonly record struct Area : IShape
{
  public Area() { }

  public string? Classes { get; init; }

  public Func<MouseEventArgs, Task>? OnClick { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOver { get; init; }

  public Func<MouseEventArgs, Task>? OnMouseOut { get; init; }

  public required Elements.Rectangle Aesthetic { get; init; }

  public SortedBuffer<(double x, double ymin, double ymax)> Points { get; }
    = new(comparer: Comparer.Instance);

  private sealed class Comparer : Comparer<(double x, double ymin, double ymax)>
  {
    public static readonly Comparer Instance = new();

    public override int Compare((double x, double ymin, double ymax) a, (double x, double ymin, double ymax) b)
            => a.x.CompareTo(b.x);
  }
}
