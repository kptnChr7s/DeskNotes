using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WpfBrush = System.Windows.Media.Brush;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfFontFamily = System.Windows.Media.FontFamily;

namespace DeskNotes.Helpers;

public static class MarkdownRenderer
{
    private static readonly Regex ListRegex = new(@"^\s*([-*+]|\d+\.)\s+(.*)$", RegexOptions.Compiled);
    private static readonly Regex AutoLinkRegex = new(@"(?<![\(""'])(https?://[^\s<>]+)", RegexOptions.Compiled);

    public static void Apply(TextBlock target, string? markdown)
    {
        try
        {
            target.Inlines.Clear();

            if (string.IsNullOrWhiteSpace(markdown))
                return;

            var lines = markdown.Replace("\r\n", "\n").Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                    target.Inlines.Add(new LineBreak());

                RenderLine(target.Inlines, lines[i], GetResourceBrush(target, "TextPrimary"));
            }
        }
        catch
        {
            target.Inlines.Clear();
            target.Inlines.Add(new Run(markdown ?? string.Empty)
            {
                Foreground = GetResourceBrush(target, "TextPrimary")
            });
        }
    }

    private static void RenderLine(InlineCollection inlines, string line, WpfBrush defaultBrush)
    {
        var match = ListRegex.Match(line);
        if (match.Success)
        {
            var marker = match.Groups[1].Value;
            var content = match.Groups[2].Value;
            var prefix = char.IsDigit(marker[0]) ? $"{marker.Split('.')[0]}." : "•";

            inlines.Add(new Run(prefix)
            {
                Foreground = GetMutedBrush(inlines),
                FontWeight = FontWeights.SemiBold
            });
            inlines.Add(new Run(" "));
            RenderInline(inlines, content, defaultBrush);
            return;
        }

        RenderInline(inlines, line, defaultBrush);
    }

    private static void RenderInline(InlineCollection inlines, string text, WpfBrush defaultBrush)
    {
        var index = 0;
        while (index < text.Length)
        {
            if (TryConsume(text, index, "```", out var codeBlockEnd))
            {
                var code = text[(index + 3)..codeBlockEnd];
                inlines.Add(CreateCodeRun(code));
                index = codeBlockEnd + 3;
                continue;
            }

            if (TryConsume(text, index, "`", out var codeEnd) && codeEnd > index + 1)
            {
                var code = text[(index + 1)..codeEnd];
                inlines.Add(CreateCodeRun(code));
                index = codeEnd + 1;
                continue;
            }

            if (TryConsume(text, index, "**", out var boldEnd) && boldEnd > index + 2)
            {
                var bold = new Bold();
                RenderInline(bold.Inlines, text[(index + 2)..boldEnd], defaultBrush);
                inlines.Add(bold);
                index = boldEnd + 2;
                continue;
            }

            if (TryConsume(text, index, "__", out var underlineBoldEnd) && underlineBoldEnd > index + 2)
            {
                var bold = new Bold();
                RenderInline(bold.Inlines, text[(index + 2)..underlineBoldEnd], defaultBrush);
                inlines.Add(bold);
                index = underlineBoldEnd + 2;
                continue;
            }

            if (TryConsume(text, index, "~~", out var strikeEnd) && strikeEnd > index + 2)
            {
                inlines.Add(new Run(text[(index + 2)..strikeEnd])
                {
                    TextDecorations = TextDecorations.Strikethrough,
                    Foreground = defaultBrush
                });
                index = strikeEnd + 2;
                continue;
            }

            if (TryLink(text, index, out var linkLabel, out var linkUrl, out var linkLength))
            {
                inlines.Add(CreateHyperlink(linkLabel, linkUrl, defaultBrush));
                index += linkLength;
                continue;
            }

            if (text[index] is '*' or '_' && index + 1 < text.Length && text[index + 1] != text[index])
            {
                var end = text.IndexOf(text[index], index + 1);
                if (end > index + 1)
                {
                    var italic = new Italic();
                    RenderInline(italic.Inlines, text[(index + 1)..end], defaultBrush);
                    inlines.Add(italic);
                    index = end + 1;
                    continue;
                }
            }

            var autoLink = AutoLinkRegex.Match(text[index..]);
            if (autoLink.Success && autoLink.Index == 0)
            {
                var url = autoLink.Value.TrimEnd('.', ',', ';', ')');
                inlines.Add(CreateHyperlink(url, url, GetLinkBrush(inlines)));
                index += url.Length;
                continue;
            }

            var nextSpecial = FindNextSpecial(text, index);
            if (nextSpecial <= index)
                nextSpecial = Math.Min(index + 1, text.Length);

            var plain = text[index..nextSpecial];
            if (plain.Length > 0)
                inlines.Add(new Run(plain) { Foreground = defaultBrush });

            index = nextSpecial;
        }
    }

    private static int FindNextSpecial(string text, int start)
    {
        var indices = new[]
        {
            IndexOrMax(text, "**", start),
            IndexOrMax(text, "__", start),
            IndexOrMax(text, "~~", start),
            IndexOrMax(text, "`", start),
            IndexOrMax(text, "[", start),
            IndexOrMax(text, "*", start),
            IndexOrMax(text, "_", start)
        };

        var auto = AutoLinkRegex.Match(text[start..]);
        if (auto.Success)
            indices = indices.Append(auto.Index + start).ToArray();

        var next = indices.Min();
        return next == int.MaxValue ? text.Length : next;
    }

    private static int IndexOrMax(string text, string value, int start)
    {
        var index = text.IndexOf(value, start, StringComparison.Ordinal);
        return index < 0 ? int.MaxValue : index;
    }

    private static bool TryConsume(string text, int index, string token, out int endIndex)
    {
        if (!text.AsSpan(index).StartsWith(token, StringComparison.Ordinal))
        {
            endIndex = -1;
            return false;
        }

        endIndex = text.IndexOf(token, index + token.Length, StringComparison.Ordinal);
        return endIndex >= 0;
    }

    private static bool TryLink(string text, int index, out string label, out string url, out int length)
    {
        label = string.Empty;
        url = string.Empty;
        length = 0;

        if (text[index] != '[')
            return false;

        var closeLabel = text.IndexOf(']', index + 1);
        if (closeLabel < 0 || closeLabel + 1 >= text.Length || text[closeLabel + 1] != '(')
            return false;

        var closeUrl = text.IndexOf(')', closeLabel + 2);
        if (closeUrl < 0)
            return false;

        label = text[(index + 1)..closeLabel];
        url = text[(closeLabel + 2)..closeUrl];
        length = closeUrl - index + 1;
        return true;
    }

    private static Run CreateCodeRun(string code) =>
        new(code)
        {
            FontFamily = new WpfFontFamily("Cascadia Mono, Consolas, Courier New"),
            Background = GetResourceBrush(null, "BackgroundElevated"),
            Foreground = GetResourceBrush(null, "AccentBlue")
        };

    private static Hyperlink CreateHyperlink(string label, string url, WpfBrush foreground)
    {
        var link = new Hyperlink(new Run(label))
        {
            NavigateUri = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null,
            Foreground = GetLinkBrush(null),
            TextDecorations = null
        };

        link.RequestNavigate += (_, e) =>
        {
            if (Uri.TryCreate(e.Uri.AbsoluteUri, UriKind.Absolute, out var target))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = target.AbsoluteUri,
                    UseShellExecute = true
                });
            }

            e.Handled = true;
        };

        return link;
    }

    private static WpfBrush GetMutedBrush(InlineCollection inlines) =>
        GetResourceBrush(null, "TextMuted");

    private static WpfBrush GetLinkBrush(InlineCollection? inlines) =>
        GetResourceBrush(null, "AccentBlue");

    private static WpfBrush GetResourceBrush(DependencyObject? context, string key)
    {
        try
        {
            return (WpfBrush)System.Windows.Application.Current.FindResource(key);
        }
        catch
        {
            return WpfBrushes.White;
        }
    }
}