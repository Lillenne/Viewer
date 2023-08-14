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
        var found = Images.FirstOrDefault(i => i.Id == img.Id);
        if (found is null)
            Images.Add(img);
        else
            Images.Remove(found);
    }

    public bool ContainsItem(Guid id) => Images.Any(i => i.Id == id);
}
