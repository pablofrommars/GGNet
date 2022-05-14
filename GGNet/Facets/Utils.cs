using static System.Math;

namespace GGNet.Facets;

public static class FacetingUtils
{
	public static (int nrows, int ncolumns) DimWrap(int n, int? nrows = null, int? ncolumns = null)
	{
		if (nrows.HasValue && ncolumns.HasValue)
		{
			return (nrows.Value, ncolumns.Value);
		}
		else if (nrows.HasValue)
		{
			return (nrows.Value, (int)Ceiling((double)n / nrows.Value));
		}
		else if (ncolumns.HasValue)
		{
			return ((int)Ceiling((double)n / ncolumns.Value), ncolumns.Value);
		}
		else
		{
			if (n <= 3)
			{
				return (n, 1);
			}
			else if (n <= 6)
			{
				return ((int)((n + 1.0) / 2.0), 2);
			}
			else if (n <= 12)
			{
				return ((int)((n + 2.0) / 3), 3);
			}
			else
			{
				var r = (int)Ceiling(Sqrt(n));
				return (r, (int)Ceiling((double)n / r));
			}
		}
	}
}