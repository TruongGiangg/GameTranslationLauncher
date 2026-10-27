using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GameTranslationLauncher.Wpf.Services;

/// <summary>
/// Tạo ảnh nền đã làm mờ cho kính Glassy và màu chủ đạo của ảnh hero.
/// </summary>
/// <remarks>
/// Downscale to a small bitmap, then box-blur it: drawn stretched back over the window this gives the
/// smooth "frosted" wash, far cheaper than a live BlurEffect with a large radius.
/// </remarks>
public static class GlassBackdropImageFilter
{
    private const int TargetWidth = 160;
    private const int BlurRadius = 5;
    private const int BlurPasses = 3;
    private const double Saturation = 1.4;
    private const double MaxBrightness = 1.1;

    // White text sits on the glass, so bright art is darkened until its mean luma is at most this (0..255).
    private const double MaxMeanLuma = 90;

    public static (BitmapSource Backdrop, Color Ambient) Create(BitmapSource hero)
    {
        ArgumentNullException.ThrowIfNull(hero);
        var scale = Math.Min(1d, TargetWidth / (double)hero.PixelWidth);
        var small = new FormatConvertedBitmap(
            new TransformedBitmap(hero, new ScaleTransform(scale, scale)),
            PixelFormats.Bgra32,
            null,
            0);
        var width = small.PixelWidth;
        var height = small.PixelHeight;
        var pixels = new byte[width * height * 4];
        small.CopyPixels(pixels, width * 4, 0);

        var ambient = ToAmbient(AverageColor(pixels));
        for (var pass = 0; pass < BlurPasses; pass++)
        {
            BoxBlur(pixels, width, height, BlurRadius);
        }

        AdjustSaturationAndBrightness(pixels, Saturation, ReadableBrightness(pixels));
        var backdrop = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        backdrop.Freeze();
        return (backdrop, ambient);
    }

    /// <summary>Separable box blur on BGRA bytes with edge clamping; three passes approximate a Gaussian.</summary>
    private static void BoxBlur(byte[] pixels, int width, int height, int radius)
    {
        var buffer = new byte[pixels.Length];
        BlurLines(pixels, buffer, width, height, radius, horizontal: true);
        BlurLines(buffer, pixels, width, height, radius, horizontal: false);
    }

    /// <summary>
    /// Pushes a mean colour to a fixed saturation floor and lightness, so dark art still yields a usable tint.
    /// </summary>
    internal static Color ToAmbient(Color mean)
    {
        var (hue, saturation, _) = ToHsl(mean);
        return FromHsl(hue, Math.Max(saturation, 0.45), 0.55);
    }

    /// <summary>Brightness factor that keeps dark art lively but never lets the glass get too light for white text.</summary>
    private static double ReadableBrightness(byte[] pixels)
    {
        var mean = AverageColor(pixels);
        var meanLuma = Luma(mean.B, mean.G, mean.R);
        return meanLuma <= 0 ? MaxBrightness : Math.Min(MaxBrightness, MaxMeanLuma / meanLuma);
    }

    private static double Luma(double blue, double green, double red) => 0.0722 * blue + 0.7152 * green + 0.2126 * red;

    private static Color AverageColor(byte[] pixels)
    {
        long blue = 0, green = 0, red = 0;
        var count = pixels.Length / 4;
        for (var i = 0; i < pixels.Length; i += 4)
        {
            blue += pixels[i];
            green += pixels[i + 1];
            red += pixels[i + 2];
        }

        return Color.FromRgb((byte)(red / count), (byte)(green / count), (byte)(blue / count));
    }

    private static void BlurLines(byte[] source, byte[] target, int width, int height, int radius, bool horizontal)
    {
        var lineCount = horizontal ? height : width;
        var length = horizontal ? width : height;
        var step = horizontal ? 4 : width * 4;
        var window = radius * 2 + 1;
        for (var line = 0; line < lineCount; line++)
        {
            var start = horizontal ? line * width * 4 : line * 4;
            for (var channel = 0; channel < 4; channel++)
            {
                var sum = 0;
                for (var k = -radius; k <= radius; k++)
                {
                    sum += source[start + Math.Clamp(k, 0, length - 1) * step + channel];
                }

                for (var i = 0; i < length; i++)
                {
                    target[start + i * step + channel] = (byte)(sum / window);
                    sum += source[start + Math.Min(i + radius + 1, length - 1) * step + channel]
                           - source[start + Math.Max(i - radius, 0) * step + channel];
                }
            }
        }
    }

    private static void AdjustSaturationAndBrightness(byte[] pixels, double saturation, double brightness)
    {
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var luma = Luma(pixels[i], pixels[i + 1], pixels[i + 2]);
            for (var channel = 0; channel < 3; channel++)
            {
                var value = (luma + (pixels[i + channel] - luma) * saturation) * brightness;
                pixels[i + channel] = (byte)Math.Clamp(value, 0, 255);
            }
        }
    }

    private static (double Hue, double Saturation, double Lightness) ToHsl(Color color)
    {
        double r = color.R / 255d, g = color.G / 255d, b = color.B / 255d;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var lightness = (max + min) / 2;
        if (max == min)
        {
            return (0, 0, lightness);
        }

        var delta = max - min;
        var saturation = delta / (1 - Math.Abs(2 * lightness - 1));
        var hue = max == r ? (g - b) / delta % 6 : max == g ? (b - r) / delta + 2 : (r - g) / delta + 4;
        return ((hue * 60 + 360) % 360, saturation, lightness);
    }

    private static Color FromHsl(double hue, double saturation, double lightness)
    {
        var chroma = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var x = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = lightness - chroma / 2;
        var (r, g, b) = hue switch
        {
            < 60 => (chroma, x, 0d),
            < 120 => (x, chroma, 0d),
            < 180 => (0d, chroma, x),
            < 240 => (0d, x, chroma),
            < 300 => (x, 0d, chroma),
            _ => (chroma, 0d, x)
        };
        return Color.FromRgb(ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    private static byte ToByte(double value) => (byte)Math.Round(Math.Clamp(value, 0, 1) * 255);
}
