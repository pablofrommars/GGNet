using GGNet.Common;

namespace GGNet.Elements;

public record Line : IElement
{
	public double Width { get; set; }

	public string Fill { get; set; } = default!;

	public double Alpha { get; set; }

	public LineType LineType { get; set; }
}