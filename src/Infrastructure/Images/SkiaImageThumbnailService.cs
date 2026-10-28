using SkiaSharp;
using skestock.Application.Common.Interfaces;

namespace skestock.Infrastructure.Images;

public class SkiaImageThumbnailService : IImageThumbnailService
{
    private const int MaxEdge = 256;
    private const int WebpQuality = 80;
    private const long MaxPixels = 40_000_000;

    public bool CanDecode(Stream image)
    {
        using var codec = SKCodec.Create(NonOwning(image));
        // The codec only reads the header; cap the pixel count so the Worker's full decode can't exhaust memory.
        return codec is not null && (long)codec.Info.Width * codec.Info.Height <= MaxPixels;
    }

    public byte[] CreateThumbnail(Stream image)
    {
        // Decoding through the codec yields the first frame of a GIF.
        using var codec = SKCodec.Create(NonOwning(image))
            ?? throw new InvalidDataException("The stream is not a decodable image.");
        using var decoded = SKBitmap.Decode(codec)
            ?? throw new InvalidDataException("The image could not be decoded.");
        // Browsers honor EXIF orientation on the full image, so the thumbnail must match.
        using var oriented = ApplyOrigin(decoded, codec.EncodedOrigin);
        var source = oriented ?? decoded;

        var scale = Math.Min(1d, (double)MaxEdge / Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));

        using var resized = scale < 1d
            ? source.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            : source.Copy();
        using var skImage = SKImage.FromBitmap(resized);
        using var data = skImage.Encode(SKEncodedImageFormat.Webp, WebpQuality);
        return data.ToArray();
    }

    // Returns null when no transform is needed.
    private static SKBitmap? ApplyOrigin(SKBitmap source, SKEncodedOrigin origin)
    {
        float w = source.Width, h = source.Height;
        var (matrix, swap) = origin switch
        {
            SKEncodedOrigin.TopRight => (new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1), false),
            SKEncodedOrigin.BottomRight => (new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1), false),
            SKEncodedOrigin.BottomLeft => (new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1), false),
            SKEncodedOrigin.LeftTop => (new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1), true),
            SKEncodedOrigin.RightTop => (new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1), true),
            SKEncodedOrigin.RightBottom => (new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1), true),
            SKEncodedOrigin.LeftBottom => (new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1), true),
            _ => (SKMatrix.Identity, false)
        };
        if (matrix.IsIdentity)
            return null;

        var target = new SKBitmap(swap ? source.Height : source.Width, swap ? source.Width : source.Height);
        using var canvas = new SKCanvas(target);
        canvas.SetMatrix(matrix);
        canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
        return target;
    }

    // SKCodec.Create(Stream) takes ownership and disposes the stream; callers keep theirs.
    private static SKManagedStream NonOwning(Stream image) => new(image, disposeManagedStream: false);
}
