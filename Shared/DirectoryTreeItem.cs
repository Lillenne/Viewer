namespace Viewer.Shared;

public class DirectoryTreeItem
{
    public required string DirectoryName { get; init; }
    public required bool HasSubDirectories { get; init; }
}