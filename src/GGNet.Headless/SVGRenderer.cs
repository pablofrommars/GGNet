namespace GGNet.Headless;

[SuppressMessage("Usage", "BL0006:Do not use RenderTree types", Justification = "GGNet Headless")]
internal sealed class SVGRenderer
{
	private static readonly HtmlEncoder encoder = HtmlEncoder.Default;

	private static readonly Regex scopeAttribute = new(@"\sb-[a-zA-Z0-9]+(?=[\s/>])", RegexOptions.Compiled);

	private static readonly HashSet<string> selfClosingElements = new(StringComparer.OrdinalIgnoreCase)
		{
            //"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
            "line", "circle", "rect", "path", "stop"
		};

	// Static export serializes the svg element only: the surrounding div and the
	// loading indicator belong to the interactive component experience.
	public static void Render(HeadlessRenderer renderer, int componentId, TextWriter writer)
	{
		var context = new Context(renderer, writer);

		if (!FindAndRenderSvg(context, renderer.GetCurrentRenderTreeFrames(componentId)))
		{
			throw new InvalidOperationException("No svg element found in the rendered component");
		}
	}

	private static bool FindAndRenderSvg(Context context, ArrayRange<RenderTreeFrame> frames)
		=> FindAndRenderSvg(context, frames, 0, frames.Count);

	private static bool FindAndRenderSvg(Context context, ArrayRange<RenderTreeFrame> frames, int position, int count)
	{
		var end = position + count;
		var i = position;

		while (i < end)
		{
			ref var frame = ref frames.Array[i];

			switch (frame.FrameType)
			{
				case RenderTreeFrameType.Element:
					if (string.Equals(frame.ElementName, "svg", StringComparison.OrdinalIgnoreCase))
					{
						RenderElement(context, frames, i);
						return true;
					}

					if (FindAndRenderSvg(context, frames, i + 1, frame.ElementSubtreeLength - 1))
					{
						return true;
					}

					i += frame.ElementSubtreeLength;
					break;

				case RenderTreeFrameType.Component:
					if (FindAndRenderSvg(context, context.Renderer.GetCurrentRenderTreeFrames(frame.ComponentId)))
					{
						return true;
					}

					i += frame.ComponentSubtreeLength;
					break;

				case RenderTreeFrameType.Region:
					if (FindAndRenderSvg(context, frames, i + 1, frame.RegionSubtreeLength - 1))
					{
						return true;
					}

					i += frame.RegionSubtreeLength;
					break;

				default:
					i++;
					break;
			}
		}

		return false;
	}

	private static int RenderFrames(Context context, ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
	{
		var nextPosition = position;
		var endPosition = position + maxElements;
		while (position < endPosition)
		{
			nextPosition = RenderCore(context, frames, position);
			if (position == nextPosition)
			{
				throw new InvalidOperationException("We didn't consume any input.");
			}
			position = nextPosition;
		}

		return nextPosition;
	}

	private static int RenderCore(Context context, ArrayRange<RenderTreeFrame> frames, int position)
	{
		ref var frame = ref frames.Array[position];
		switch (frame.FrameType)
		{
			case RenderTreeFrameType.Element:
				return RenderElement(context, frames, position);
			case RenderTreeFrameType.Attribute:
				throw new InvalidOperationException($"Attributes should only be encountered within {nameof(RenderElement)}");
			case RenderTreeFrameType.Text:
				{
					// Razor source-file whitespace is not document structure.
					var text = frame.TextContent.Trim();
					if (text.Length > 0)
					{
						context.Write(encoder.Encode(text));
						context.WroteText = true;
					}
					return ++position;
				}
			case RenderTreeFrameType.Markup:
				{
					// Static razor fragments arrive as raw markup with the scoped-CSS
					// marker baked in; strip it like the attribute-frame path does.
					var markup = scopeAttribute.Replace(frame.MarkupContent, "").Trim();
					if (markup.Length > 0)
					{
						if (markup[0] == '<')
						{
							context.WriteIndent();
							context.WroteElement = true;
						}
						else
						{
							context.WroteText = true;
						}

						context.Write(markup);
					}
					return ++position;
				}
			case RenderTreeFrameType.Component:
				return RenderChildComponent(context, frames, position);
			case RenderTreeFrameType.Region:
				return RenderFrames(context, frames, position + 1, frame.RegionSubtreeLength - 1);
			case RenderTreeFrameType.ElementReferenceCapture:
			case RenderTreeFrameType.ComponentReferenceCapture:
				return ++position;
			default:
				throw new InvalidOperationException($"Invalid element frame type '{frame.FrameType}'.");
		}
	}

	private static int RenderChildComponent(Context context, ArrayRange<RenderTreeFrame> frames, int position)
	{
		ref var frame = ref frames.Array[position];
		var childFrames = context.Renderer.GetCurrentRenderTreeFrames(frame.ComponentId);
		RenderFrames(context, childFrames, 0, childFrames.Count);
		return position + frame.ComponentSubtreeLength;
	}

	private static int RenderElement(Context context, ArrayRange<RenderTreeFrame> frames, int position)
	{
		ref var frame = ref frames.Array[position];
		context.WriteIndent();
		context.Write("<");
		context.Write(frame.ElementName);
		var afterAttributes = RenderAttributes(context, frames, position + 1, frame.ElementSubtreeLength - 1, out var capturedValueAttribute);

		// When we see an <option> as a descendant of a <select>, and the option's "value" attribute matches the
		// "value" attribute on the <select>, then we auto-add the "selected" attribute to that option. This is
		// a way of converting Blazor's select binding feature to regular static HTML.
		if (context.ClosestSelectValueAsString is not null
			&& string.Equals(frame.ElementName, "option", StringComparison.OrdinalIgnoreCase)
			&& string.Equals(capturedValueAttribute, context.ClosestSelectValueAsString, StringComparison.Ordinal))
		{
			context.Write(" selected");
		}

		var remainingElements = frame.ElementSubtreeLength + position - afterAttributes;
		if (remainingElements > 0)
		{
			context.Write(">");

			var isSelect = string.Equals(frame.ElementName, "select", StringComparison.OrdinalIgnoreCase);
			if (isSelect)
			{
				context.ClosestSelectValueAsString = capturedValueAttribute;
			}

			context.WroteElement = false;
			context.WroteText = false;
			context.Depth++;

			var afterElement = RenderChildren(context, frames, afterAttributes, remainingElements);

			context.Depth--;
			var hadElementChildren = context.WroteElement;

			if (isSelect)
			{
				// There's no concept of nested <select> elements, so as soon as we're exiting one of them,
				// we can safely say there is no longer any value for this
				context.ClosestSelectValueAsString = null;
			}

			// Close on its own line only when the content was elements; text content stays inline.
			if (hadElementChildren)
			{
				context.WroteText = false;
				context.WriteIndent();
			}

			context.Write("</");
			context.Write(frame.ElementName);
			context.Write(">");
			context.WroteElement = true;
			context.WroteText = false;
			Debug.Assert(afterElement == position + frame.ElementSubtreeLength);
			return afterElement;
		}
		else
		{
			if (selfClosingElements.Contains(frame.ElementName))
			{
				context.Write(" />");
			}
			else
			{
				context.Write(">");
				context.Write("</");
				context.Write(frame.ElementName);
				context.Write(">");
			}
			context.WroteElement = true;
			context.WroteText = false;
			Debug.Assert(afterAttributes == position + frame.ElementSubtreeLength);
			return afterAttributes;
		}
	}

	private static int RenderChildren(Context context, ArrayRange<RenderTreeFrame> frames, int position, int maxElements)
	{
		if (maxElements == 0)
		{
			return position;
		}

		return RenderFrames(context, frames, position, maxElements);
	}

	private static int RenderAttributes(Context context, ArrayRange<RenderTreeFrame> frames, int position, int maxElements, out string? capturedValueAttribute)
	{
		capturedValueAttribute = null;

		if (maxElements == 0)
		{
			return position;
		}

		for (var i = 0; i < maxElements; i++)
		{
			var candidateIndex = position + i;
			ref var frame = ref frames.Array[candidateIndex];
			if (frame.FrameType != RenderTreeFrameType.Attribute)
			{
				return candidateIndex;
			}

			if (frame.AttributeName.Equals("value", StringComparison.OrdinalIgnoreCase))
			{
				capturedValueAttribute = frame.AttributeValue as string;
			}

			// Blazor scoped-CSS marker attributes: meaningless outside the component,
			// and valueless attributes break XML validity.
			if (frame.AttributeName.StartsWith("b-", StringComparison.Ordinal))
			{
				continue;
			}

			switch (frame.AttributeValue)
			{
				case bool flag when flag:
					context.Write(" ");
					context.Write(frame.AttributeName);
					break;
				case string value:
					context.Write(" ");
					context.Write(frame.AttributeName);
					context.Write("=");
					context.Write("\"");
					context.Write(encoder.Encode(value));
					context.Write("\"");
					break;
				default:
					break;
			}
		}

		return position + maxElements;
	}

	private sealed class Context(HeadlessRenderer renderer, TextWriter writer)
	{
		private readonly TextWriter writer = writer;

		private bool first = true;

		public HeadlessRenderer Renderer { get; } = renderer;

		public string? ClosestSelectValueAsString { get; set; }

		public int Depth { get; set; }

		public bool WroteElement { get; set; }

		public bool WroteText { get; set; }

		public void Write(string str) => writer.Write(str);

		// Newline + depth tabs before structural content; suppressed for the root
		// and when flowing after inline text.
		public void WriteIndent()
		{
			if (first)
			{
				first = false;
				return;
			}

			if (WroteText)
			{
				return;
			}

			writer.Write('\n');

			for (var i = 0; i < Depth; i++)
			{
				writer.Write('\t');
			}
		}
	}
}
