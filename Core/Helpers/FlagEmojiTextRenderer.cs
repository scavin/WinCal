using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinCal.Core.Helpers;

/// <summary>
/// Renders country flag emoji as inline images because Windows does not ship
/// colored glyphs for regional-indicator flag sequences.
/// </summary>
public static class FlagEmojiTextRenderer
{
    private const string FlagArchiveResource = "pack://application:,,,/Assets/twemoji-flags.zip";
    private const int BlackFlag = 0x1F3F4;
    private const int VariationSelector16 = 0xFE0F;
    private const int RegionalIndicatorA = 0x1F1E6;
    private const int RegionalIndicatorZ = 0x1F1FF;
    private const int TagBase = 0xE0000;
    private const int TagStart = 0xE0020;
    private const int TagEnd = 0xE007E;
    private const int CancelTag = 0xE007F;
    private const string EnglandFlagKey = "1f3f4-e0067-e0062-e0065-e006e-e0067-e007f";
    private const string ScotlandFlagKey = "1f3f4-e0067-e0062-e0073-e0063-e0074-e007f";
    private const string WalesFlagKey = "1f3f4-e0067-e0062-e0077-e006c-e0073-e007f";
    private static readonly Dictionary<string, ImageSource?> FlagSourceCache = new();
    private static readonly Dictionary<string, (string ImageKey, string FallbackText)> PlainBlackFlagTeamMap = new()
    {
        ["英格兰"] = (EnglandFlagKey, "GBENG"),
        ["England"] = (EnglandFlagKey, "GBENG"),
        ["苏格兰"] = (ScotlandFlagKey, "GBSCT"),
        ["Scotland"] = (ScotlandFlagKey, "GBSCT"),
        ["威尔士"] = (WalesFlagKey, "GBWLS"),
        ["Wales"] = (WalesFlagKey, "GBWLS")
    };

    public static void SetText(TextBlock textBlock, string? text)
    {
        text ??= string.Empty;

        if (!ContainsCountryFlagEmoji(text))
        {
            textBlock.Inlines.Clear();
            textBlock.Text = text;
            return;
        }

        textBlock.Inlines.Clear();

        var buffer = new StringBuilder();
        var runes = text.EnumerateRunes().ToArray();

        for (var i = 0; i < runes.Length; i++)
        {
            if (i + 1 < runes.Length
                && TryGetRegionalIndicatorLetter(runes[i], out var first)
                && TryGetRegionalIndicatorLetter(runes[i + 1], out var second))
            {
                AppendRun(textBlock, buffer);
                var imageKey = $"{runes[i].Value:x}-{runes[i + 1].Value:x}";
                textBlock.Inlines.Add(CreateFlagInline(imageKey, $"{first}{second}", textBlock.FontSize));
                i++;
                continue;
            }

            if (TryGetTagFlag(runes, i, out var tagFlagImageKey, out var fallbackText, out var consumedRunes))
            {
                AppendRun(textBlock, buffer);
                textBlock.Inlines.Add(CreateFlagInline(tagFlagImageKey, fallbackText, textBlock.FontSize));
                i += consumedRunes - 1;
                continue;
            }

            buffer.Append(runes[i].ToString());
        }

        AppendRun(textBlock, buffer);
    }

    private static bool ContainsCountryFlagEmoji(string text)
    {
        var previousWasRegionalIndicator = false;
        var runes = text.EnumerateRunes().ToArray();

        for (var i = 0; i < runes.Length; i++)
        {
            var rune = runes[i];
            var isRegionalIndicator = TryGetRegionalIndicatorLetter(rune, out _);
            if (previousWasRegionalIndicator && isRegionalIndicator)
                return true;

            if (TryGetTagFlag(runes, i, out _, out _, out _))
                return true;

            previousWasRegionalIndicator = isRegionalIndicator;
        }

        return false;
    }

    private static bool TryGetRegionalIndicatorLetter(Rune rune, out char letter)
    {
        var value = rune.Value;
        if (value is < RegionalIndicatorA or > RegionalIndicatorZ)
        {
            letter = '\0';
            return false;
        }

        letter = (char)('A' + value - RegionalIndicatorA);
        return true;
    }

    private static bool TryGetTagFlag(
        Rune[] runes,
        int startIndex,
        out string imageKey,
        out string fallbackText,
        out int consumedRunes)
    {
        imageKey = string.Empty;
        fallbackText = string.Empty;
        consumedRunes = 0;

        if (runes[startIndex].Value != BlackFlag)
            return false;

        var keyParts = new List<string> { BlackFlag.ToString("x") };
        var tagText = new StringBuilder();
        var tagStartIndex = startIndex + 1;

        if (tagStartIndex < runes.Length && runes[tagStartIndex].Value == VariationSelector16)
            tagStartIndex++;

        if (TryGetPlainBlackFlagTeam(runes, tagStartIndex, out imageKey, out fallbackText))
        {
            consumedRunes = tagStartIndex - startIndex;
            return true;
        }

        for (var i = tagStartIndex; i < runes.Length; i++)
        {
            var value = runes[i].Value;
            keyParts.Add(value.ToString("x"));

            if (value == CancelTag)
            {
                if (tagText.Length == 0)
                    return false;

                imageKey = string.Join("-", keyParts);
                fallbackText = tagText.ToString().ToUpperInvariant();
                consumedRunes = i - startIndex + 1;
                return true;
            }

            if (value is < TagStart or > TagEnd)
                return false;

            tagText.Append((char)(value - TagBase));
        }

        return false;
    }

    private static bool TryGetPlainBlackFlagTeam(
        Rune[] runes,
        int teamNameStartIndex,
        out string imageKey,
        out string fallbackText)
    {
        imageKey = string.Empty;
        fallbackText = string.Empty;

        foreach (var (teamName, flag) in PlainBlackFlagTeamMap)
        {
            if (!StartsWithText(runes, teamNameStartIndex, teamName))
                continue;

            imageKey = flag.ImageKey;
            fallbackText = flag.FallbackText;
            return true;
        }

        return false;
    }

    private static bool StartsWithText(Rune[] runes, int startIndex, string text)
    {
        var textRunes = text.EnumerateRunes().ToArray();
        if (startIndex + textRunes.Length > runes.Length)
            return false;

        for (var i = 0; i < textRunes.Length; i++)
        {
            if (runes[startIndex + i] != textRunes[i])
                return false;
        }

        return true;
    }

    private static void AppendRun(TextBlock textBlock, StringBuilder buffer)
    {
        if (buffer.Length == 0)
            return;

        textBlock.Inlines.Add(new Run(buffer.ToString()));
        buffer.Clear();
    }

    private static InlineUIContainer CreateFlagInline(string imageKey, string fallbackText, double fontSize)
    {
        var width = Math.Max(14, fontSize * 1.35);
        var height = Math.Max(10, fontSize);

        var source = GetFlagSource(imageKey);

        var fallback = new TextBlock
        {
            Text = fallbackText,
            FontSize = Math.Max(8, fontSize * 0.75),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = source == null ? Visibility.Visible : Visibility.Collapsed
        };

        var image = new Image
        {
            Width = width,
            Height = height,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Source = source,
            ToolTip = fallbackText,
            Visibility = source == null ? Visibility.Collapsed : Visibility.Visible
        };

        image.ImageFailed += (_, _) =>
        {
            image.Visibility = Visibility.Collapsed;
            fallback.Visibility = Visibility.Visible;
        };

        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

        var container = new Grid
        {
            Width = width,
            Height = height,
            ToolTip = fallbackText
        };
        container.Children.Add(fallback);
        container.Children.Add(image);

        return new InlineUIContainer(container)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
    }

    private static ImageSource? GetFlagSource(string imageKey)
    {
        if (FlagSourceCache.TryGetValue(imageKey, out var cached))
            return cached;

        var source = CreateFlagSource(imageKey);
        FlagSourceCache[imageKey] = source;
        return source;
    }

    private static ImageSource? CreateFlagSource(string imageKey)
    {
        var archiveResource = Application.GetResourceStream(new Uri(FlagArchiveResource, UriKind.Absolute));
        if (archiveResource == null)
            return null;

        using var archive = new ZipArchive(archiveResource.Stream, ZipArchiveMode.Read);
        var entry = archive.GetEntry($"{imageKey}.png");
        if (entry == null)
            return null;

        using var entryStream = entry.Open();
        using var memory = new MemoryStream();
        entryStream.CopyTo(memory);
        memory.Position = 0;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = memory;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
