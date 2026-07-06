using GGNet.Data;
using GGNet.Facets;
using GGNet.Scales;

namespace GGNet.Geoms.VLine;

internal sealed class VLine<T, TX, TY> : Geom<T, TX, TY>
	where TX : struct
	where TY : struct
{
	public VLine(
		IReadOnlyList<T> source,
		Func<T, TX> x,
		Func<T, string> label)
		: base(source, null)
	{
		Selectors = new()
		{
			X = x,
			Label = label
		};
	}

	public Selectors<T, TX> Selectors { get; }

	public Positions<T> Positions { get; } = new();

	public required Elements.Line Line { get; set; }

	public required Elements.Text Text { get; set; }

	public override void Init<T1>(Panel<T1, TX, TY> panel, Facet<T1>? facet)
	{
		base.Init(panel, facet);

		Positions.X = new PositionMapping<T, TX>(Selectors.X, panel.X);
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
