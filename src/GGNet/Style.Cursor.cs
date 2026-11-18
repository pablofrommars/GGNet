namespace GGNet;

public sealed partial class Style
{
	/// <summary>In-chart cursor label layout. Assign to Style.Cursor to opt in; null preserves legacy cursor geometry.</summary>
	public sealed class StyleCursor
	{
		/// <summary>Label size in GGNet style units; null inherits the X axis text size.</summary>
		public Size? FontSize { get; set; }

		/// <summary>Show the label only on the first panel of each shared-X column. Local cursors keep their own label.</summary>
		public bool FirstPanelOnly { get; set; }

		/// <summary>Inset from the panel's top and horizontal edges, in SVG units.</summary>
		public double Padding { get; set; } = 4;

		/// <summary>Space below the rendered label before the cursor line begins, in SVG units.</summary>
		public double Gap { get; set; } = 4;
	}
}
