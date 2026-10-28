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
        using var source = SKBitmap.Decode(codec)
            ?? throw new InvalidDataException("The image could not be decoded.");

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

    // SKCodec.Create(Stream) takes ownership and disposes the stream; callers keep theirs.
    private static SKManagedStream NonOwning(Stream image) => new(image, disposeManagedStream: false);
}
