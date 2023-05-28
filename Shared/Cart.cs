using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace Viewer.Shared;

public class Cart
{
    public ObservableCollection<ImageID> Images { get; } = new();

    public void AddOrRemove(ImageID? img)
    {
        if (img is null)
            return;
        if (!Images.Contains(img))
            Images.Add(img);
        else
            Images.Remove(img);
    }
    
    
}
