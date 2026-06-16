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
    private const int RegionalIndicatorA = 0x1F1E6;
    private const int RegionalIndicatorZ = 0x1F1FF;
    private static readonly Dictionary<string, ImageSource?> FlagSourceCache = new();

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

            buffer.Append(runes[i].ToString());
        }

        AppendRun(textBlock, buffer);
    }

    private static bool ContainsCountryFlagEmoji(string text)
    {
        var previousWasRegionalIndicator = false;

        foreach (var rune in text.EnumerateRunes())
        {
            var isRegionalIndicator = TryGetRegionalIndicatorLetter(rune, out _);
            if (previousWasRegionalIndicator && isRegionalIndicator)
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
