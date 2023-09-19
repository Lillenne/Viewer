using MudBlazor;
using Viewer.Client.Components;
using Viewer.Shared;

namespace Viewer.Client.ServiceClients;

public class ImagePopUpProvider
{
    private readonly IImageClient _client;
    private readonly IDialogService _ds;
    private readonly IAuthClient _authClient;

    public ImagePopUpProvider(IImageClient client, IDialogService ds, IAuthClient authClient)
    {
        _client = client;
        _ds = ds;
        _authClient = authClient;
    }

    public async Task CreatePopUp(NamedUri img)
    {
        var loggedIn = _authClient .GetIsLoggedIn().ConfigureAwait(false);
        var src = await _client.GetImage(
            new GetImageRequest()
            {
                Id = img.Id,
                Width = -1,
                Height = -1,
            }
        );
        var disp = src ?? img;
        var lin = await loggedIn;
        var opts = new DialogOptions()
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            NoHeader = lin
        };
        var parameters = new DialogParameters { { "Source", disp } };
        var title = lin ? string.Empty : "If you were logged in, this would be the same image you clicked on!";
        _ = await _ds.ShowAsync<ImagePopUp>(title, parameters, opts);
    }
}
