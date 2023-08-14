using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace Viewer.Shared;

public class DirectoryTreeItem
{
    public Guid? Source { get; set; }
    public string DirectoryName { get; set; }
    [JsonIgnore] public DirectoryTreeItem? Parent { get; set; }
    public IList<DirectoryTreeItem> Subdirectories { get; set; }
    public int FileCount { get; set; }

    public DirectoryTreeItem(string directoryName = "")
    {
        DirectoryName = directoryName;
        Subdirectories = new List<DirectoryTreeItem>();
    }

    [SetsRequiredMembers]
    [JsonConstructor]
    public DirectoryTreeItem(string directoryName, DirectoryTreeItem? parent, IList<DirectoryTreeItem> subdirectories)
    {
        Parent = parent;
        DirectoryName = directoryName;
        Subdirectories = subdirectories;
    }

    public string GetShortName() => DirectoryName.EndsWith(Path.DirectorySeparatorChar) 
        ? Path.GetFileName(DirectoryName.AsSpan(0, DirectoryName.Length - 1)).ToString()
        : Path.GetFileName(DirectoryName);

    /// <summary>
    /// Gets the fully qualified directory name (key), excluding the root node.
    /// </summary>
    /// <returns>The fully qualified directory name excluding the root node or null if the node has no parent</returns>
    public string? KeyFromRoot()
    {
        if (Parent is null)
            return null;
        StringBuilder sb = new();
        KeyFromRoot(this, sb, false);
        return sb.ToString();
    }

    public string Key()
    {
        if (Parent is null)
            return DirectoryName.EndsWith(Path.DirectorySeparatorChar) ? DirectoryName : $"{DirectoryName}{Path.DirectorySeparatorChar}";
        StringBuilder sb = new();
        KeyFromRoot(this, sb, true);
        return sb.ToString();
    }

    private static void KeyFromRoot(DirectoryTreeItem item, StringBuilder sb, bool includeRoot)
    {
        // Recurse to root node
        if (item.Parent is not null)
            KeyFromRoot(item.Parent, sb, includeRoot);
        // Ignore the root node
        if (item.Parent is null && !includeRoot)
            return;
        // Write all other nodes
        sb.Append(item.DirectoryName);
        if (!item.DirectoryName.EndsWith(Path.DirectorySeparatorChar))
            sb.Append(Path.DirectorySeparatorChar);
    }

    public DirectoryTreeItem Root()
    {
        HashSet<DirectoryTreeItem>? cycleDetect = null;
        var root = this;
        while (root.Parent is not null)
        {
            cycleDetect ??= new();
            if (cycleDetect.Contains(root))
                throw new InvalidOperationException("Attempted to find root of cyclic structure");
            cycleDetect.Add(root);
            root = root.Parent;
        }

        return root;
    }

    public static void RestoreDoublyLinks(IEnumerable<DirectoryTreeItem> item)
    {
        foreach (var i in item)
            RestoreDoublyLinks(i);
    }
    public static void RestoreDoublyLinks(DirectoryTreeItem item)
    {
        foreach (var child in item.Subdirectories)
        {
            child.Parent = item;
            RestoreDoublyLinks(child);
        }
    }

    public bool IsRoot() => Parent is null;
}
