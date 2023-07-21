using Viewer.Shared;
using Viewer.Shared.Requests;
using Viewer.Shared.Services;

namespace Viewer.Server.Services;

public class ImageServiceStub : IImageService
{
    private const int FILE_COUNT = 20;
    public Task<GetImagesResponse> GetImages(GetImagesRequest request)
    {
        var img = GetImg();
        var imgs = Enumerable.Range(0, FILE_COUNT).Select(i => img).ToList();
        var result = new GetImagesResponse() { Images = imgs };
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? dir)
    {
        var ti = new DirectoryTreeItem("Folder 1") { FileCount = FILE_COUNT };
        var tf = new DirectoryTreeItem("Folder 2", new[] { ti }) { FileCount = FILE_COUNT };
        var tb = new DirectoryTreeItem("Folder 3") { FileCount = FILE_COUNT };

        return Task.FromResult((IReadOnlyList<DirectoryTreeItem>)(new[] { ti, tf, tb }));
    }

    public Task<ImageId> GetImage()
    {
        return Task.FromResult(GetImg());
    }

    private static ImageId GetImg()
    {
        return new ImageId
        {
            Name = "Random image",
            Url =
                @"https://upload.wikimedia.org/wikipedia/commons/thumb/6/66/SMPTE_Color_Bars.svg/1200px-SMPTE_Color_Bars.svg.png"
        };
    }

    public Task<ImageId> GetImage(GetImageRequest request) => GetImage();

    public Task<ImageId> Upload(ImageUpload image) => Task.FromResult(GetImg());

    public Task<IEnumerable<ImageId>> Upload(IEnumerable<ImageUpload> images) => Task.FromResult(images.Select(_ => GetImg()));
}
