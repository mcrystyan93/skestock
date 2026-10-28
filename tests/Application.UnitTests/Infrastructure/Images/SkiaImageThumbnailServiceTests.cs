using NUnit.Framework;
using Shouldly;
using SkiaSharp;
using skestock.Infrastructure.Images;

namespace skestock.Application.UnitTests.Infrastructure.Images;

public class SkiaImageThumbnailServiceTests
{
    private readonly SkiaImageThumbnailService _service = new();

    private static MemoryStream Encode(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return new MemoryStream(data.ToArray());
    }

    private static SKCodec Decode(byte[] bytes) => SKCodec.Create(new MemoryStream(bytes))!;

    // Inserts a minimal big-endian EXIF APP1 segment carrying only the Orientation tag right after SOI.
    private static MemoryStream WithExifOrientation(MemoryStream jpeg, byte orientation)
    {
        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22,
            0x45, 0x78, 0x69, 0x66, 0x00, 0x00,
            0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08,
            0x00, 0x01,
            0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, orientation, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00
        ];
        var original = jpeg.ToArray();
        return new MemoryStream([.. original[..2], .. app1, .. original[2..]]);
    }

    [TestCase((byte)6, 128, 256)]
    [TestCase((byte)8, 128, 256)]
    [TestCase((byte)3, 256, 128)]
    [TestCase((byte)1, 256, 128)]
    public void CreateThumbnail_AppliesExifOrientation(byte orientation, int expectedWidth, int expectedHeight)
    {
        using var source = WithExifOrientation(Encode(600, 300, SKEncodedImageFormat.Jpeg), orientation);

        var bytes = _service.CreateThumbnail(source);

        using var codec = Decode(bytes);
        codec.Info.Width.ShouldBe(expectedWidth);
        codec.Info.Height.ShouldBe(expectedHeight);
    }

    [Test]
    public void CanDecode_ExcessivePixelCount_ReturnsFalse()
    {
        // Gray8 keeps the 49 MP test bitmap at ~49 MB; a flat fill compresses to a tiny PNG.
        using var bitmap = new SKBitmap(new SKImageInfo(7000, 7000, SKColorType.Gray8, SKAlphaType.Opaque));
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var source = new MemoryStream(data.ToArray());

        _service.CanDecode(source).ShouldBeFalse();
    }

    [Test]
    public void CreateThumbnail_LargeImage_ResizesToMax256AsWebp()
    {
        using var source = Encode(600, 300, SKEncodedImageFormat.Png);

        var bytes = _service.CreateThumbnail(source);

        using var codec = Decode(bytes);
        codec.EncodedFormat.ShouldBe(SKEncodedImageFormat.Webp);
        codec.Info.Width.ShouldBe(256);
        codec.Info.Height.ShouldBe(128);
    }

    [Test]
    public void CreateThumbnail_TallImage_ScalesByHeight()
    {
        using var source = Encode(100, 512, SKEncodedImageFormat.Jpeg);

        using var codec = Decode(_service.CreateThumbnail(source));

        codec.Info.Height.ShouldBe(256);
        codec.Info.Width.ShouldBe(50);
    }

    [Test]
    public void CreateThumbnail_SmallImage_IsNotEnlarged()
    {
        using var source = Encode(100, 50, SKEncodedImageFormat.Png);

        using var codec = Decode(_service.CreateThumbnail(source));

        codec.Info.Width.ShouldBe(100);
        codec.Info.Height.ShouldBe(50);
    }

    [TestCase(SKEncodedImageFormat.Jpeg)]
    [TestCase(SKEncodedImageFormat.Png)]
    [TestCase(SKEncodedImageFormat.Webp)]
    public void CanDecode_SupportedFormats_ReturnsTrue(SKEncodedImageFormat format)
    {
        using var source = Encode(20, 20, format);

        _service.CanDecode(source).ShouldBeTrue();
    }

    [Test]
    public void CanDecode_Gif_ReturnsTrue_AndThumbnailUsesFirstFrame()
    {
        using var source = new MemoryStream(Convert.FromBase64String(
            "R0lGODlhAQABAIAAAP///wAAACwAAAAAAQABAAACAkQBADs="));

        _service.CanDecode(source).ShouldBeTrue();
        source.Position = 0;
        using var codec = Decode(_service.CreateThumbnail(source));
        codec.EncodedFormat.ShouldBe(SKEncodedImageFormat.Webp);
        codec.Info.Width.ShouldBe(1);
    }

    [Test]
    public void CanDecode_Garbage_ReturnsFalse_AndCreateThumbnailThrows()
    {
        using var source = new MemoryStream("not an image at all"u8.ToArray());

        _service.CanDecode(source).ShouldBeFalse();
        source.Position = 0;
        Should.Throw<InvalidDataException>(() => _service.CreateThumbnail(source));
    }
}
