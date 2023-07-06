using SixLabors.ImageSharp.Formats.Png;
using Viewer.Shared;
using Viewer.Shared.Requests;

namespace Viewer.Server.Services;

public class AppFileImageService : IImageService
{
    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName)
    {
        if (!Path.IsPathRooted(directoryName))
        {
            directoryName = Path.Join(AppDomain.CurrentDomain.BaseDirectory, directoryName);
        }

        if (!Path.Exists(directoryName))
        {
            return Task.FromResult(
                    (IReadOnlyList<DirectoryTreeItem>)Array.Empty<DirectoryTreeItem>()
                );
        }

        return Task.FromResult(
                (IReadOnlyList<DirectoryTreeItem>)
                    Directory
                        .EnumerateDirectories(directoryName)
                        .Select(d =>
                        {
                            return new DirectoryTreeItem
                            {
                                HasSubDirectories = Directory.EnumerateDirectories(d).Any(),
                                DirectoryName = d
                            };
                        })
                        .ToList()
            );
    }

    public Task<GetImagesResponse> GetImages(GetImagesRequest request)
    {
        List<ImageId> imageIds = Directory
            .EnumerateFiles(
                AppDomain.CurrentDomain.BaseDirectory,
                "*",
                SearchOption.TopDirectoryOnly
            )
            .Select(GetImg)
            .ToList();
        return Task.FromResult(new GetImagesResponse() { Images = imageIds });
    }

    private static string GetRelativePath(string str)
    {
        if (!str.Contains(AppDomain.CurrentDomain.BaseDirectory))
        {
            return str;
        }

        int l = AppDomain.CurrentDomain.BaseDirectory.Length;
        return str[l..];
    }

    public Task<ImageId> GetImage(GetImageRequest request)
    {
        using var fs = File.OpenRead(request.Name);
        using var img = Image.Load(fs);
        img.ResizeImage(request.Width, request.Height);
        return Task.FromResult(new ImageId
        {
            Guid = Guid.NewGuid(),
            Name = Path.GetFileNameWithoutExtension(request.Name),
            Url = img.ToBase64String(PngFormat.Instance)
        });
    }

    private static ImageId GetImg(string f)
    {
        return new ImageId
        {
            Url = GetRelativePath(f),
            Name = Path.GetFileNameWithoutExtension(f),
            Guid = Guid.NewGuid() // TODO
        };
    }
}
