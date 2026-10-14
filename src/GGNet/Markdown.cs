using System.Security;

namespace GGNet;

internal static partial class Markdown
{
	[GeneratedRegex(@"\*\*(?<bold>.+?)\*\*|\*(?<italic>.+?)\*|~(?<sub>.+?)~|\^(?<sup>.+?)\^", RegexOptions.Singleline)]
	private static partial Regex TokenRegex();

	private static readonly ObjectPool<StringBuilder> pool = new DefaultObjectPoolProvider().CreateStringBuilderPool();

	private static string Escape(string text) => SecurityElement.Escape(text);

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
					sb.Append(Escape(markdown[lastIndex..match.Index]));
				}

				if (match.Groups["bold"].Success)
				{
					sb.Append(CultureInfo.InvariantCulture, $"<tspan font-weight=\"bold\">{Escape(match.Groups["bold"].Value)}</tspan>");
				}
				else if (match.Groups["italic"].Success)
				{
					sb.Append(CultureInfo.InvariantCulture, $"<tspan font-style=\"italic\">{Escape(match.Groups["italic"].Value)}</tspan>");
				}
				else if (match.Groups["sub"].Success)
				{
					sb.Append(CultureInfo.InvariantCulture, $"<tspan baseline-shift=\"sub\" font-size=\"0.7em\">{Escape(match.Groups["sub"].Value)}</tspan>");
				}
				else if (match.Groups["sup"].Success)
				{
					sb.Append(CultureInfo.InvariantCulture, $"<tspan baseline-shift=\"super\" font-size=\"0.7em\">{Escape(match.Groups["sup"].Value)}</tspan>");
				}

				lastIndex = match.Index + match.Length;
			}

			if (lastIndex < markdown.Length)
			{
				sb.Append(Escape(markdown[lastIndex..]));
			}

			return sb.ToString();
		}
		finally
		{
			pool.Return(sb);
		}
	}
}
