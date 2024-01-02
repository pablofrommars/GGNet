namespace GGNet.Geoms.ABLine;

internal sealed class ABLine<T> : Geom<T, double, double>
{
	private readonly (bool x, bool y) transformation;

	public ABLine(
		Source<T> source,
		Func<T, double> a,
		Func<T, double> b,
		Func<T, string>? label,
		(bool x, bool y)? transformation = null)
		: base(source, null, false)
	{
		this.transformation = transformation ?? (true, true);

		Selectors = new()
		{
			A = a,
			B = b,
			Label = label
		};
	}

	public Selectors<T> Selectors { get; }

	public Elements.Line Line { get; set; } = default!; 

	public Elements.Text Text { get; set; } = default!;

	public override void Train(T item) { }

	protected override void Shape(T item, bool flip)
	{
		var a = Selectors.A(item);
		var b = Selectors.B(item);

		string? label = null;
		if (Selectors.Label is not null)
		{
			label = Selectors.Label(item);
		}

		if (string.IsNullOrEmpty(label))
		{
			return;
		}

		Layer.Add(new Shapes.ABLine
		{
			A = a,
			B = b,
			Transformation = transformation,
			Label = label,
			Line = Line,
			Text = Text
		});
	}
}