using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Viewer.Shared.Users;

namespace Viewer.Shared;

public class ImageUpload
{
    public required string? Prefix { get; init; }
    public required string Name { get; init; }
    public required Stream Image { get; init; }
    public required Visibility Visibility { get; init; }
    public required UserDto Owner { get; init; }
    
    public ImageUpload(){}

    [SetsRequiredMembers]
    public ImageUpload(string? prefix, string name, Stream image, Visibility visibility, UserDto owner)
    {
        Name = name;
        Image = image;
        Visibility = visibility;
        Owner = owner;
    }
}

public class UploadHeader
{
    public string? Prefix { get; set; }
    public IList<UploadItemHeader> Items { get; init; } = new List<UploadItemHeader>();
}

public class UploadItemHeader
{
    [Required] public string? Name { get; set; } = string.Empty;
    public Visibility Visibility { get; set; }
}