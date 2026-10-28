using Moq;
using skestock.Application.Common.Interfaces;
using skestock.Application.Storage.Interfaces;

namespace skestock.Application.UnitTests.Features.Items.Images;

/// <summary>Inert collaborators for item tests that do not exercise the image path.</summary>
internal static class ItemImageTestSupport
{
    public static IUser User(Guid? id = null)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(id ?? Guid.NewGuid());
        return user.Object;
    }

    public static IBlobStorageService Blobs() => Mock.Of<IBlobStorageService>();

    public static IImageThumbnailService Thumbnails() => Mock.Of<IImageThumbnailService>();
}
