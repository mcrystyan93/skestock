namespace skestock.Application.Common.Interfaces;

public interface IImageThumbnailService
{
    /// <summary>True when the stream holds an image the server can decode.</summary>
    bool CanDecode(Stream image);

    /// <summary>Returns a WebP thumbnail (max edge 256 px, never enlarged). Throws <see cref="InvalidDataException"/> when the image cannot be decoded.</summary>
    byte[] CreateThumbnail(Stream image);
}
