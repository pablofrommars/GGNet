namespace GGNet.Facets;

public class Faceting2D<T, TRow, TColumn> : Faceting<T>
{
	private readonly SortedBuffer<TRow> rows = new(4, 1);
	private readonly SortedBuffer<TColumn> columns = new(4, 1);

	private readonly Func<T, TRow> row;
	private readonly Func<T, TColumn> column;

	public Faceting2D(Func<T, TRow> row, Func<T, TColumn> column, bool freeX, bool freeY)
		: base(freeX, freeY)
	{
		this.row = row;
		this.column = column;
	}

	public override bool Strip => true;

	public override void Train(T item)
	{
		rows.Add(row(item));
		columns.Add(column(item));
	}

	public override void Set()
	{
		NRows = rows.Count;
		NColumns = columns.Count;

		N = NRows * NColumns;
	}

	public override (int row, int column) Map(T item)
	{
		int r = rows.IndexOf(row(item));
		int c = columns.IndexOf(column(item));

		return (r, c);
	}

	public override void Clear()
	{
		rows.Clear();
		columns.Clear();
	}

	public override (Facet<T> facet, bool showX, bool showY)[] Facets(Theme theme)
	{
		var facets = new (Facet<T> facet, bool showX, bool showY)[N];

		var i = 0;

		for (var r = 0; r < NRows; r++)
		{
			for (var c = 0; c < NColumns; c++)
			{
				var xStrip = r == 0
					? columns[c].ToString()
					: string.Empty;

				var yStrip = c == (NColumns - 1)
					? rows[r].ToString()
					: string.Empty;

				var showY = theme.Axis.Y == Position.Left
					? c == 0
					: c == (NColumns - 1);

				var showX = r == (NRows - 1);

				facets[i++] = (new Facet<T>(this, (r, c), xStrip, yStrip), showX, showY);
			}
		}

		return facets;
	}
}