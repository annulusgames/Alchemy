using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Alchemy.Docs;

internal static class ImageCrop
{
    // Match Unity Color(0,1,1) rendered near pure cyan.
    const byte TargetR = 0;
    const byte TargetG = 255;
    const byte TargetB = 255;
    const int ChannelTolerance = 40;
    const float MinCyanRowRatio = 0.35f;
    // Skip the unlabeled __docCaptureStart serialized field under the top cyan line.
    const int TopContentInsetPx = 22;

    public static void CropFile(string inputPath, string outputPath)
    {
        using var image = Image.Load<Rgba32>(inputPath);
        var bands = FindCyanBands(image);
        if (bands.Count < 2)
        {
            throw new InvalidOperationException(
                $"Expected at least two cyan capture bands in '{inputPath}', found {bands.Count}.");
        }

        var top = bands[0].End + TopContentInsetPx;
        var bottom = bands[^1].Start;
        if (bottom <= top + 2)
        {
            throw new InvalidOperationException(
                $"Cyan capture bands did not enclose a usable region in '{inputPath}' " +
                $"(top={top}, bottom={bottom}, inset={TopContentInsetPx}).");
        }

        var height = bottom - top;
        image.Mutate(ctx => ctx.Crop(new Rectangle(0, top, image.Width, height)));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        SavePngWithRetry(image, outputPath);
    }

    static void SavePngWithRetry(Image<Rgba32> image, string outputPath)
    {
        const int attempts = 8;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                image.SaveAsPng(outputPath);
                return;
            }
            catch (IOException) when (attempt < attempts)
            {
                Thread.Sleep(250 * attempt);
            }
        }
    }

    public static bool TryCropFile(string inputPath, string outputPath, out string? error)
    {
        try
        {
            CropFile(inputPath, outputPath);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    static List<(int Start, int End)> FindCyanBands(Image<Rgba32> image)
    {
        var cyanRows = new List<int>();
        for (var y = 0; y < image.Height; y++)
        {
            var cyan = 0;
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                if (IsCyan(pixel))
                {
                    cyan++;
                }
            }

            if (cyan >= image.Width * MinCyanRowRatio)
            {
                cyanRows.Add(y);
            }
        }

        var bands = new List<(int Start, int End)>();
        if (cyanRows.Count == 0)
        {
            return bands;
        }

        var start = cyanRows[0];
        var prev = cyanRows[0];
        for (var i = 1; i < cyanRows.Count; i++)
        {
            if (cyanRows[i] > prev + 1)
            {
                bands.Add((start, prev + 1));
                start = cyanRows[i];
            }

            prev = cyanRows[i];
        }

        bands.Add((start, prev + 1));
        return bands;
    }

    static bool IsCyan(Rgba32 pixel) =>
        Math.Abs(pixel.R - TargetR) <= ChannelTolerance &&
        Math.Abs(pixel.G - TargetG) <= ChannelTolerance &&
        Math.Abs(pixel.B - TargetB) <= ChannelTolerance &&
        pixel.A >= 200;
}
