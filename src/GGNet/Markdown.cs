namespace GGNet;

public static partial class Markdown
{
	[GeneratedRegex(@"\*\*(?<bold>.+?)\*\*|\*(?<italic>.+?)\*|~(?<sub>.+?)~|\^(?<sup>.+?)\^", RegexOptions.Singleline)]
	private static partial Regex TokenRegex();

	private static readonly ObjectPool<StringBuilder> pool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

	// * Usage: <text>@((MarkupString)@Markdown.Text("**bold** *italic* ~subscript~ ^superscript^", 10, 20))</text>
	public static string Text([StringSyntax("Markdown")] string markdown)
	{
		var sb = pool.Get();
		try
		{
			var lastIndex = 0;
			foreach (Match match in TokenRegex().Matches(markdown))
			{
				if (match.Index > lastIndex)
				{
					sb.Append(markdown[lastIndex..match.Index]);
				}

				if (match.Groups["bold"].Success)
				{
					sb.Append($"<tspan font-weight=\"bold\">{match.Groups["bold"].Value}</tspan>");
				}
				else if (match.Groups["italic"].Success)
				{
					sb.Append($"<tspan font-style=\"italic\">{match.Groups["italic"].Value}</tspan>");
				}
				else if (match.Groups["sub"].Success)
				{
					sb.Append($"<tspan baseline-shift=\"sub\" font-size=\"0.7em\">{match.Groups["sub"].Value}</tspan>");
				}
				else if (match.Groups["sup"].Success)
				{
					sb.Append($"<tspan baseline-shift=\"super\" font-size=\"0.7em\">{match.Groups["sup"].Value}</tspan>");
				}

				lastIndex = match.Index + match.Length;
			}

			if (lastIndex < markdown.Length)
			{
				sb.Append(markdown[lastIndex..]);
			}

			return sb.ToString();
		}
		finally
		{
			pool.Return(sb);
		}
	}
}