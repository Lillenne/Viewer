namespace Viewer.Shared.Services;
using System.Diagnostics.CodeAnalysis;

public class ImageUpload
{
    public required string Name { get; init; }
    public required Stream Image { get; init; }
    
    public ImageUpload(){}

    [SetsRequiredMembers]
    public ImageUpload(string name, Stream image)
    {
        Name = name;
        Image = image;
    }
}
