using GGNet.Data;
using GGNet.Facets;

namespace GGNet.Geoms.VLine;

internal sealed class VLine<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public VLine(
		Source<T> source,
		Func<T, TX> x,
		Func<T, string> label)
		: base(source, null, false)
	{
		Selectors = new()
		{
			X = x,
			Label = label
		};
	}

	public Selectors<T, TX> Selectors { get; }

	public Positions<T> Positions { get; } = new();

	public Elements.Line Line { get; set; } = default!;

	public Elements.Text Text { get; set; } = default!;

	public override void Init<T1, TX1, TY1>(Panel<T1, TX1, TY1> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		Positions.X = XMapping(Selectors.X, panel.X);
	}

	public override void Train(T item)
	{
		Positions.X.Train(item);
	}

	protected override void Shape(T item, bool flip)
	{
		var x = Positions.X.Map(item);

		string? label = null;
		if (Selectors.Label is not null)
		{
			label = Selectors.Label(item);
		}

		if (string.IsNullOrEmpty(label))
		{
			return;
		}

		Layer.Add(new Shapes.VLine
		{
			X = x,
			Label = label,
			Line = Line,
			Text = Text
		});
	}
}