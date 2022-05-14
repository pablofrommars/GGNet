namespace GGNet.Static;

[SuppressMessage("Usage", "BL0006:Do not use RenderTree types", Justification = "<Pending>")]
internal class SVGRenderer
{
	private static readonly HtmlEncoder encoder = HtmlEncoder.Default;

	private static readonly HashSet<string> selfClosingElements = new(StringComparer.OrdinalIgnoreCase)
		{
            //"area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
            "line", "circle", "rect", "path", "stop"
		};

	public static void Render(StaticRenderer renderer, int componentId, TextWriter writer)
	{
		var frames = renderer.GetCurrentRenderTreeFrames(componentId);
		var context = new Context(renderer, writer);
		var newPosition = RenderFrames(context, frames, 0, frames.Count);
		Debug.Assert(newPosition == frames.Count);
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
				context.Write(encoder.Encode(frame.TextContent));
				return ++position;
			case RenderTreeFrameType.Markup:
				context.Write(frame.MarkupContent);
				return ++position;
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

			var afterElement = RenderChildren(context, frames, afterAttributes, remainingElements);

			if (isSelect)
			{
				// There's no concept of nested <select> elements, so as soon as we're exiting one of them,
				// we can safely say there is no longer any value for this
				context.ClosestSelectValueAsString = null;
			}

			context.Write("</");
			context.Write(frame.ElementName);
			context.Write(">");
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

			/*
			if (frame.AttributeEventHandlerId > 0)
			{
				context.Write($" {frame.AttributeName}=\"{frame.AttributeEventHandlerId}\"");
				continue;
			}
			*/

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

	private sealed class Context
	{
		private readonly TextWriter writer;

		public Context(StaticRenderer renderer, TextWriter writer)
		{
			Renderer = renderer;

			this.writer = writer;
		}

		public StaticRenderer Renderer { get; }

		public string? ClosestSelectValueAsString { get; set; }

		public void Write(string str) => writer.Write(str);
	}
}