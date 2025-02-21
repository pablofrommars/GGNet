using GGNet.Data;
using GGNet.Facets;

namespace GGNet.Geoms.HLine;

internal sealed class HLine<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public HLine(
		IReadOnlyList<T> source,
		Func<T, TY> y,
		Func<T, string> label)
		: base(source, null, false)
	{
		Selectors = new()
		{
			Y = y,
			Label = label
		};
	}

	public Selectors<T, TY> Selectors { get; }

	public Positions<T> Positions { get; } = new();

	public Elements.Line Line { get; set; } = default!;

	public Elements.Text Text { get; set; } = default!;

	public override void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		Positions.Y = YMapping(Selectors.Y, panel.Y);
	}

	public override void Train(T item)
	{
		Positions.Y.Train(item);
	}

	protected override void Shape(T item, bool flip)
	{
		if (Selectors.Label is null)
		{
			return;
		}

		Layer.Add(new Shapes.HLine
		{
			Y = Positions.Y.Map(item),
			Label = Selectors.Label.Invoke(item),
			Line = Line,
			Text = Text
		});
	}
}