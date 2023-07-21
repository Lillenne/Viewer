using MudBlazor;
using Viewer.Client;
using Viewer.Shared;
using Viewer.Shared.Requests;

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

    public async Task CreatePopUp(ImageId img)
    {
        var src = await _client.GetImage(
            new GetImageRequest()
            {
                Name = img.Name,
                Width = -1,
                Height = -1,
            }
        );
        var disp = src is not null ? src : img;
        var opts = new DialogOptions()
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            NoHeader = true
        };
        var parameters = new DialogParameters { { "Source", disp } };
        _ = _ds.Show<ImagePopUp>(string.Empty, parameters, opts);
    }
}
