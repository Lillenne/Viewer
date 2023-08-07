using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Viewer.Shared;

public class DirectoryTreeItem
{
    public string DirectoryName { get; private set; }
    public IList<DirectoryTreeItem> Subdirectories { get; set; }
    public int FileCount { get; set; }

    public DirectoryTreeItem(string directoryName)
    {
        DirectoryName = directoryName;
        Subdirectories = new List<DirectoryTreeItem>();
    }

    [SetsRequiredMembers]
    [JsonConstructor]
    public DirectoryTreeItem(string directoryName, IList<DirectoryTreeItem> subdirectories)
    {
        DirectoryName = directoryName;
        Subdirectories = subdirectories;
    }

    public string GetShortName() =>
        !string.IsNullOrEmpty(DirectoryName)
        ? (DirectoryName.Length == 1 ? DirectoryName : Path.GetFileName(DirectoryName.AsSpan(0, DirectoryName.Length - 1)).ToString())
        : "/";
}
