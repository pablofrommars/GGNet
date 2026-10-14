namespace GGNet.Headless;

internal static class SvgExtractor
{
	private static readonly HashSet<string> selfClosingElements = new(StringComparer.OrdinalIgnoreCase)
	{
		"line", "circle", "rect", "path", "stop"
	};

	public static void Write(string html, TextWriter writer)
	{
		var parser = new HtmlParser();

		using var document = parser.ParseDocument(html);

		var svg = Chart(document) ?? throw new GGNetInternalException("No svg element found in the rendered component.");

		WriteElement(svg, writer, 0, false);
	}

	private static IElement? Chart(IDocument document)
	{
		var root = document.Body?.FirstElementChild;

		if (root is null)
		{
			return null;
		}

		foreach (var child in root.Children)
		{
			if (string.Equals(child.LocalName, "svg", StringComparison.OrdinalIgnoreCase))
			{
				return child;
			}
		}

		return null;
	}

	private static void WriteElement(IElement element, TextWriter writer, int depth, bool indent)
	{
		if (indent)
		{
			WriteIndent(writer, depth);
		}

		writer.Write('<');
		writer.Write(element.LocalName);

		foreach (var attribute in element.Attributes)
		{
			if (attribute.Name.StartsWith("b-", StringComparison.Ordinal))
			{
				continue;
			}

			writer.Write(' ');
			writer.Write(attribute.Name);
			writer.Write("=\"");
			writer.Write(SecurityElement.Escape(attribute.Value));
			writer.Write('"');
		}

		var content = Content(element);

		if (content.Count == 0)
		{
			if (selfClosingElements.Contains(element.LocalName))
			{
				writer.Write(" />");
			}
			else
			{
				writer.Write("></");
				writer.Write(element.LocalName);
				writer.Write('>');
			}

			return;
		}

		writer.Write('>');

		var wroteText = false;

		for (var i = 0; i < content.Count; i++)
		{
			if (content[i] is IElement child)
			{
				WriteElement(child, writer, depth + 1, !wroteText);
				wroteText = false;

				continue;
			}

			var text = ((IText)content[i]).Data;

			if (i == 0)
			{
				text = text.TrimStart();
			}

			if (i == content.Count - 1)
			{
				text = text.TrimEnd();
			}

			writer.Write(SecurityElement.Escape(text));
			wroteText = true;
		}

		if (content[0] is IElement)
		{
			WriteIndent(writer, depth);
		}

		writer.Write("</");
		writer.Write(element.LocalName);
		writer.Write('>');
	}

	private static List<INode> Content(IElement element)
	{
		var content = new List<INode>();

		foreach (var node in element.ChildNodes)
		{
			if (node is IText text)
			{
				if (!string.IsNullOrWhiteSpace(text.Data))
				{
					content.Add(text);
				}
			}
			else if (node is IElement child)
			{
				content.Add(child);
			}
		}

		return content;
	}

	private static void WriteIndent(TextWriter writer, int depth)
	{
		writer.Write('\n');

		for (var i = 0; i < depth; i++)
		{
			writer.Write('\t');
		}
	}
}
