using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp.Formats.Png;
using Viewer.Shared;
using Viewer.Shared.Requests;

namespace Viewer.Server.Services;

public class AppFileImageService : IImageService
{
    private static string BaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pictures");

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName)
    {
        if (!GetValidPath(ref directoryName))
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
        var name = request.Directory;
        if (!GetValidPath(ref name))
        {
            return Task.FromResult(new GetImagesResponse()
            {
                Images = new List<ImageId>()
            });
        }
        List<ImageId> imageIds = GetImageFiles(name)
            .Select(f => GetIdFromB64Str(f, LoadImage(f, request.Width, request.Height)))
            .ToList();
        return Task.FromResult(new GetImagesResponse() { Images = imageIds });
    }

    private static string GetRelativePath(string str)
    {
        if (!str.Contains(BaseDirectory))
        {
            return str;
        }

        int start = BaseDirectory.Length + 1;
        if (str.Length <= start)
            throw new ArgumentException("Invalid path");

        return str[start..];
    }

    public Task<ImageId> GetImage(GetImageRequest request)
    {
        var name = request.Name;
        if (!GetValidPath(ref name))
        {
            return Task.FromException<ImageId>(new FileNotFoundException(name));
        }
        return Task.FromResult(new ImageId
        {
            Guid = Guid.NewGuid(),
            Name = GetRelativePath(name),
            Url = LoadImage(name, request.Width, request.Height)
        });
    }
    
    private static string LoadImage(string name, int width, int height)
    {
        using var fs = File.OpenRead(name);
        using var img = Image.Load(fs);
        img.ResizeImage(width, height);
        return img.ToBase64String(PngFormat.Instance);
    }
    
    private static string LoadImage(string name)
    {
        using var fs = File.OpenRead(name);
        using var img = Image.Load(fs);
        return img.ToBase64String(PngFormat.Instance);
    }

    private static ImageId GetIdFromB64Str(string path, string f)
    {
        return new ImageId
        {
            Url = f,
            //Url = GetRelativePath(f),
            Name = GetRelativePath(path),
            Guid = Guid.NewGuid() // TODO
        };
    }

    private static bool GetValidPath([NotNullWhen(true)] ref string? directoryName)
    {
        directoryName ??= BaseDirectory;
        if (!directoryName.Contains(BaseDirectory))
        {
            directoryName = Path.Join(BaseDirectory, directoryName);
        }

        return Path.Exists(directoryName);
    }

    private static readonly string[] exts = { "png", "jpg", "jpeg", "tif", "tiff" };

    private static IEnumerable<string> GetImageFiles(string dir) => GetFileTypesInFolder(dir, exts);


    private static IEnumerable<string> GetFileTypesInFolder(string dir, IEnumerable<string> exts)
    {
        foreach (var ext in exts)
        {
            var files = Directory.EnumerateFiles(dir, $"*.{ext}", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                yield return file;
            }
        }
    }
}
