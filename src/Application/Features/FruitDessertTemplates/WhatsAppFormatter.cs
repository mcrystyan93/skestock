using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace skestock.Application.Features.FruitDessertTemplates;

/// <summary>
/// Whitelist sanitizer and HTML -> WhatsApp markup converter for message templates. Output is always rebuilt
/// from tokens, never filtered in place, so attributes and unknown tags cannot survive.
/// </summary>
public static partial class WhatsAppFormatter
{
    private static readonly HashSet<string> Allowed =
        ["p", "br", "strong", "b", "em", "i", "s", "strike", "code", "ul", "ol", "li"];

    private static readonly Dictionary<string, string> Marks = new()
    {
        ["strong"] = "*", ["b"] = "*", ["em"] = "_", ["i"] = "_", ["s"] = "~", ["strike"] = "~", ["code"] = "`"
    };

    private enum Kind { Text, Open, Close }

    private readonly record struct Token(Kind Kind, string Value);

    [GeneratedRegex(@"<!--.*?(?:-->|$)|<(/?)([a-zA-Z][a-zA-Z0-9]*)((?:""[^""]*""|'[^']*'|[^>])*)>?", RegexOptions.Singleline)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"<(?:/?[a-zA-Z]|!--)")]
    private static partial Regex AnyTagRegex();

    // Comments are dropped, script/style content is dropped entirely, other tags keep only their name.
    private static IEnumerable<Token> Tokenize(string html)
    {
        var pos = 0;
        while (pos < html.Length)
        {
            var m = TokenRegex().Match(html, pos);
            var textEnd = m.Success ? m.Index : html.Length;
            if (textEnd > pos)
            {
                yield return new Token(Kind.Text, WebUtility.HtmlDecode(html[pos..textEnd]));
            }

            if (!m.Success)
            {
                yield break;
            }

            pos = m.Index + m.Length;
            if (m.Value.StartsWith("<!--", StringComparison.Ordinal))
            {
                continue;
            }

            var name = m.Groups[2].Value.ToLowerInvariant();
            var closing = m.Groups[1].Length > 0;
            if (!closing && name is "script" or "style")
            {
                var end = html.IndexOf("</" + name, pos, StringComparison.OrdinalIgnoreCase);
                if (end < 0)
                {
                    yield break;
                }

                var close = html.IndexOf('>', end);
                pos = close < 0 ? html.Length : close + 1;
                continue;
            }

            if (Allowed.Contains(name))
            {
                yield return new Token(closing ? Kind.Close : Kind.Open, name);
            }
        }
    }

    public static string Sanitize(string html)
    {
        // Tagless = legacy plain text, stored raw (ToWhatsApp does not decode it).
        if (!AnyTagRegex().IsMatch(html))
        {
            return html;
        }

        var sb = new StringBuilder();
        foreach (var t in Tokenize(html))
        {
            switch (t.Kind)
            {
                case Kind.Text: sb.Append(WebUtility.HtmlEncode(t.Value)); break;
                case Kind.Open: sb.Append('<').Append(t.Value).Append('>'); break;
                case Kind.Close when t.Value != "br": sb.Append("</").Append(t.Value).Append('>'); break;
            }
        }

        return sb.ToString();
    }

    public static string ToWhatsApp(string html)
    {
        // No tag = legacy plain text (seeded bodies): keep it byte for byte.
        if (!AnyTagRegex().IsMatch(html))
        {
            return html;
        }

        var root = new StringBuilder();
        var stack = new List<(string Tag, StringBuilder Sb)>();
        var lists = new Stack<(bool Ordered, int Index)>();
        var hasBlock = false;
        var suppressSep = false;

        StringBuilder Cur() => stack.Count > 0 ? stack[^1].Sb : root;

        void BlockStart(string? prefix = null)
        {
            if (hasBlock && !suppressSep)
            {
                Cur().Append('\n');
            }

            hasBlock = true;
            suppressSep = prefix is not null;
            if (prefix is not null)
            {
                Cur().Append(prefix);
            }
        }

        void CloseMark(int index)
        {
            var (tag, sb) = stack[index];
            stack.RemoveRange(index, stack.Count - index);
            var inner = sb.ToString();
            var core = inner.Trim(' ', '\t', '\n');
            var target = Cur();
            if (core.Length == 0)
            {
                target.Append(inner);
                return;
            }

            var lead = inner[..inner.IndexOf(core[0])];
            var marker = Marks[tag];
            target.Append(lead).Append(marker).Append(core).Append(marker).Append(inner[(lead.Length + core.Length)..]);
        }

        foreach (var t in Tokenize(html))
        {
            switch (t.Kind)
            {
                case Kind.Text:
                    suppressSep = false;
                    Cur().Append(t.Value.Replace(' ', ' '));
                    break;
                case Kind.Open when Marks.ContainsKey(t.Value):
                    stack.Add((t.Value, new StringBuilder()));
                    break;
                case Kind.Close when Marks.ContainsKey(t.Value):
                    var idx = stack.FindLastIndex(e => e.Tag == t.Value);
                    if (idx >= 0)
                    {
                        CloseMark(idx);
                    }

                    break;
                case Kind.Open when t.Value == "p":
                    BlockStart();
                    break;
                case Kind.Open when t.Value == "br":
                    suppressSep = false;
                    Cur().Append('\n');
                    break;
                case Kind.Open when t.Value is "ul" or "ol":
                    lists.Push((t.Value == "ol", 0));
                    break;
                case Kind.Close when t.Value is "ul" or "ol":
                    if (lists.Count > 0)
                    {
                        lists.Pop();
                    }

                    break;
                case Kind.Open when t.Value == "li":
                    var inList = lists.Count > 0;
                    var (ordered, n) = inList ? lists.Pop() : (false, 0);
                    n++;
                    if (inList)
                    {
                        lists.Push((ordered, n));
                    }

                    BlockStart(ordered ? $"{n}. " : "- ");
                    break;
            }
        }

        while (stack.Count > 0)
        {
            CloseMark(stack.Count - 1);
        }

        return root.ToString();
    }
}
