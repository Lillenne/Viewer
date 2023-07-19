using System.Diagnostics.CodeAnalysis;
using SixLabors.ImageSharp.Formats.Png;
using Viewer.Shared;
using Viewer.Shared.Requests;
using Viewer.Shared.Services;

namespace Viewer.Server.Services;

public class AppFileImageService : IImageService
{
    private static readonly string BaseDirectory = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Pictures"
    );

    public Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string? directoryName)
    {
        if (!GetValidPath(ref directoryName))
        {
            return Task.FromResult(
                (IReadOnlyList<DirectoryTreeItem>)Array.Empty<DirectoryTreeItem>()
            );
        }

        // var l = new List<DirectoryTreeItem>();
        // foreach (var dir in Directory.EnumerateDirectories(directoryName, "*", SearchOption.TopDirectoryOnly))
        // {
        //     var d = new DirectoryTreeItem(dir);
        //     FillSubdirs(d);
        //     l.Add(d);
        // }

        if (!directoryName.EndsWith(Path.DirectorySeparatorChar))
            directoryName += Path.DirectorySeparatorChar;
        var t = new DirectoryTreeItem(directoryName);
        FillSubdirs(t);
        return Task.FromResult((IReadOnlyList<DirectoryTreeItem>)new List<DirectoryTreeItem> { t });
    }

    private static void FillSubdirs(DirectoryTreeItem dir)
    {
        foreach (
            var subdir in Directory.EnumerateDirectories(
                dir.DirectoryName,
                "*",
                SearchOption.TopDirectoryOnly
            )
        )
        {
            var item = subdir.EndsWith(Path.DirectorySeparatorChar) ? new DirectoryTreeItem(subdir) : new DirectoryTreeItem(subdir + Path.DirectorySeparatorChar);
            dir.Subdirectories.Add(item);
            FillSubdirs(item);
        }
    }

    public Task<GetImagesResponse> GetImages(GetImagesRequest request)
    {
        var name = request.Directory;
        if (!GetValidPath(ref name))
        {
            return Task.FromResult(new GetImagesResponse() { Images = new List<ImageId>() });
        }
        var enu = GetImageFiles(name).Skip(request.StartIndex);
        if (request.TakeNumber > 0)
            enu = enu.Take(request.TakeNumber);

        List<ImageId> imageIds = enu.Select(f => GetIdFromB64Str(f, LoadImage(f, request.Width, request.Height))).ToList();
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
        return Task.FromResult(
            new ImageId
            {
                Name = GetRelativePath(name),
                Url = LoadImage(name, request.Width, request.Height)
            }
        );
    }

    private static string LoadImage(string name, int width, int height)
    {
        using var fs = File.OpenRead(name);
        using var img = Image.Load(fs);
        img.ResizeImage(width, height);
        return img.ToBase64String(PngFormat.Instance);
    }

    private static ImageId GetIdFromB64Str(string path, string f)
    {
        return new ImageId { Url = f, Name = GetRelativePath(path), };
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

    private static IEnumerable<string> GetImageFiles(string dir)
    {
        return GetFileTypesInFolder(dir, exts);
    }

    private static IEnumerable<string> GetFileTypesInFolder(
        string dir,
        IEnumerable<string> exts,
        SearchOption option = SearchOption.TopDirectoryOnly
    )
    {
        foreach (var ext in exts)
        {
            var files = Directory.EnumerateFiles(dir, $"*.{ext}", option);
            foreach (var file in files)
            {
                yield return file;
            }
        }
    }

    public async Task Upload(ImageUpload image)
    {
        var path = Path.Combine(BaseDirectory, image.Name);
        await File.WriteAllBytesAsync(path, image.Image);
    }

    public async Task Upload(IEnumerable<ImageUpload> images)
    {
        foreach (var img in images)
        {
            await Upload(img);
        }
    }
}
