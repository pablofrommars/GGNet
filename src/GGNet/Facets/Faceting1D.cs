﻿using GGNet.Buffers;

namespace GGNet.Facets;

public sealed class Faceting1D<T, TKey>(Func<T, TKey> selector, bool freeX, bool freeY, int? nrows, int? ncolumns) : Faceting<T>(freeX, freeY)
{
	private readonly SortedBuffer<TKey> buffer = new(8, 1);

	private readonly Func<T, TKey> selector = selector;

	private readonly int? nrows = nrows;
	private readonly int? ncolumns = ncolumns;

    public override bool Strip => false;

	public override void Train(T item) => buffer.Add(selector(item));

	public override void Set()
	{
		N = buffer.Count;

		var dim = FacetingUtils.DimWrap(N, nrows, ncolumns);

		NRows = dim.nrows;
		NColumns = dim.ncolumns;
	}

	public override (int row, int column) Map(T item)
	{
		int n = buffer.IndexOf(selector(item));

		return ((int)((double)n / NColumns), n % NColumns);
	}

	public override void Clear() => buffer.Clear();

	public override (Facet<T> facet, bool showX, bool showY)[] Facets(Style style)
	{
		var n = buffer.Count;
		var facets = new(Facet<T> facet, bool showX, bool showY)[n];

		var r = 0;
		var c = 0;

		for (var i = 0; i < n; i++)
		{
			var xStrip = buffer[i]?.ToString();

			var showY = style.Axis.Y == Position.Left ? c == 0 : (c == (NColumns - 1) || i == (n - 1));

			if (r == (NRows - 1))
			{
				facets[i] = (new(this, (r, c), xStrip), true, showY);

				c++;
			}
			else
			{
				var showX = i == (n - 1);

				facets[i] = (new(this, (r, c), xStrip), showX, showY);

				if (++c == NColumns)
				{
					r++;
					c = 0;
				}
			}
		}

		return facets;
	}
}
