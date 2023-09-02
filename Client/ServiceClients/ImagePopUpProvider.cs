using MudBlazor;
using Viewer.Client.Components;
using Viewer.Shared;

namespace Viewer.Client.ServiceClients;

public class ImagePopUpProvider
{
    private readonly IImageClient _client;
    private readonly IDialogService _ds;

    public ImagePopUpProvider(IImageClient client, IDialogService ds)
    {
        _client = client;
        _ds = ds;
    }

    public async Task CreatePopUp(NamedUri img)
    {
        var src = await _client.GetImage(
            new GetImageRequest()
            {
                Id = img.Id,
                Width = -1,
                Height = -1,
            }
        );
        var disp = src ?? img;
        var opts = new DialogOptions()
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            NoHeader = true
        };
        var parameters = new DialogParameters { { "Source", disp } };
        _ = await _ds.ShowAsync<ImagePopUp>(string.Empty, parameters, opts);
    }
}
