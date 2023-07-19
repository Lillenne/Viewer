using Viewer.Shared;
using Viewer.Shared.Requests;
using Viewer.Shared.Services;

namespace Viewer.Server.Services;

public class ImageServiceStub : IImageService
{
    public Task<GetImagesResponse> GetImages(GetImagesRequest request)
    {
        var img = GetImg();
        var imgs = Enumerable.Range(0, 20).Select(i => img).ToList();
        var result = new GetImagesResponse() { Images = imgs };
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? dir)
    {
        var ti = new DirectoryTreeItem("Folder 1");
        var tf = new DirectoryTreeItem("Folder 2", new[] { ti });
        var tb = new DirectoryTreeItem("Folder 3");

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

    public Task Upload(ImageUpload image) => Task.CompletedTask;

    public Task Upload(IEnumerable<ImageUpload> images) => Task.CompletedTask;
}
