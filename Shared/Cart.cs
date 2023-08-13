using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Viewer.Shared;

public class Cart
{
    public ObservableCollection<NamedUri> Images { get; } = new();

    public void AddOrRemove(NamedUri? img)
    {
        if (img is null)
            return;
        if (!ContainsSimilar(img.Name))
            Images.Add(img);
        else
            RemoveSimilar(img.Name);
    }

    private void RemoveSimilar(string id)
    {

        var n = TryGetBaseName(id, out var bn) ? bn : id;
        var item = Images.FirstOrDefault(i => i.Name.Contains(n, StringComparison.OrdinalIgnoreCase));
        if (item is null)
            return;
        _ = Images.Remove(item);
    }

    public bool ContainsSimilar(string id)
    {
        return TryGetBaseName(id, out var baseName)
            && Images.Any(i => i.Name.Contains(baseName, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryGetBaseName(string? id, [NotNullWhen(true)] out string? baseName)
    {
        baseName = null;
        if (id is null)
            return false;
        var ext = Path.GetExtension(id);
        var idx = id.IndexOf(ext);
        if (idx < 0)
            return false;
        baseName = id[..idx];
        return true;
    }
}
