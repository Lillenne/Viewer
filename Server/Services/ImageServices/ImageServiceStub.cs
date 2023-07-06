using Viewer.Shared;
using Viewer.Shared.Requests;

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
        var ti = new DirectoryTreeItem
        {
            DirectoryName = "Folder 1",
            HasSubDirectories = true
        };
        var tf = new DirectoryTreeItem
        {
            DirectoryName = "Folder 2",
            HasSubDirectories = true
            //SubDirectories = new HashSet<DirectoryTreeItem> { ti }
        };
        var tb = new DirectoryTreeItem
        {
            DirectoryName = "Folder 3",
            HasSubDirectories = false
            //SubDirectories = new HashSet<DirectoryTreeItem> { ti, tf }
        };

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
            Guid = Guid.NewGuid(),
            Name = "Random image",
            Url =
                @"https://upload.wikimedia.org/wikipedia/commons/thumb/6/66/SMPTE_Color_Bars.svg/1200px-SMPTE_Color_Bars.svg.png"
        };
    }

    public Task<ImageId> GetImage(GetImageRequest request) => GetImage();
}
