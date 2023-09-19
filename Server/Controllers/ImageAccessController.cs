using System.Security.Claims;
using System.Text.Json;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Viewer.Server.Services;
using Viewer.Server.Services.AuthServices;
using Viewer.Server.Services.ImageServices;
using Viewer.Shared;

namespace Viewer.Server.Controllers;

public class RedirectOnPolicyFail : ActionFilterAttribute
{
    private readonly string _policy;
    private readonly string _path;

    public RedirectOnPolicyFail(string policy, string path)
    {
        _policy = policy;
        _path = path;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var auth = context.HttpContext.RequestServices.GetService<IAuthorizationService>();
        if (auth is null || string.IsNullOrEmpty(_policy))
            return;
        var authed = await auth.AuthorizeAsync(context.HttpContext.User, _policy).ConfigureAwait(false);
        if (!authed.Succeeded)
            context.Result = new RedirectResult(_path);
        await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
    }
}

[ApiController]
[Route("api/[controller]")]
public class ImageAccessController : ControllerBase
{
    private readonly ILogger<ImageAccessController> _logger;
    private readonly IImageService _service;
    private readonly IClaimsParser _identifier;
    private readonly IUserRepository _users;

    public ImageAccessController(ILogger<ImageAccessController> logger, IImageService service, IClaimsParser identifier // TODO remove parser?
        , IUserRepository users
    )
    {
        _logger = logger;
        _service = service;
        _identifier = identifier;
        _users = users;
    }

    #region GetImages

    [HttpPost]
    public async Task<ActionResult<GetImagesResponse>> GetImages(GetImagesRequest request, [FromServices] IAuthenticationService auth)
    {
        var isAuthorized = await auth.AuthenticateAsync(HttpContext, JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
        if (!isAuthorized.Succeeded)
        {
            _logger.LogInformation("Received sample GetImagesRequest");
            return await GetRandomImages(request).ConfigureAwait(false);
        }
        try
        {
            _logger.LogInformation("Received get request for {RequestSourceId}/{RequestDirectory} at w{RequestWidth} from {FindFirst}", request.SourceId, request.Directory, request.Width, HttpContext.User.FindFirst(ClaimTypes.NameIdentifier));
            var response = await _service.GetImageIds(request).ConfigureAwait(false);
            return new ActionResult<GetImagesResponse>(response);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in get images");
            return StatusCode(500);
        }
    }

    [HttpPost("samples")]
    public Task<ActionResult<GetImagesResponse>> GetRandomImages(GetImagesRequest request)
    {
        var n = (int)new Random().NextInt64(10, 50);
        var w = request.Width > 0 ? request.Width : 255;
        var rand = new Random();
        return Task.FromResult(new ActionResult<GetImagesResponse>(new GetImagesResponse
        {
            Images = Enumerable.Range(0, n).Select(_  => new NamedUri
            {
                Name = "Sample image",
                Id = Guid.NewGuid(),
                Uri = $"https://picsum.photos/{w}.webp?random={rand.NextInt64(1,int.MaxValue)}"
            }).ToList()
        }));
    }
    
    [HttpPost("image")]
    public async Task<ActionResult<NamedUri>> Image(GetImageRequest request, [FromServices] IAuthenticationService auth)
    {
        var isAuthorized = await auth.AuthenticateAsync(HttpContext, JwtBearerDefaults.AuthenticationScheme).ConfigureAwait(false);
        if (!isAuthorized.Succeeded)
        {
            return await SampleImage(request).ConfigureAwait(false);
        }
        try
        {
            return new ActionResult<NamedUri>(await _service.GetImageId(request).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in post files");
            return StatusCode(500);
        }
    }

    [HttpPost("image-sample")]
    public Task<ActionResult<NamedUri>> SampleImage(GetImageRequest request)
    {
        var w = request.Width > 0 ? request.Width : 600;
        return Task.FromResult(new ActionResult<NamedUri>(new NamedUri("A sample image", request.Id, $"https://picsum.photos/{w}.webp")));
    }
    
    #endregion

    #region Upload/Download

    [HttpPost("upload")]
    [Authorize(Policy = Policies.UploadPolicy)]
    public async Task<ActionResult<GetImagesResponse>> PostFiles([FromForm] string header, [FromForm] IList<IFormFile> files)
    {
        _logger.LogInformation("Received {FilesCount} file upload from {FindFirst}", files.Count, 
            HttpContext.User.FindFirst(ClaimTypes.NameIdentifier));
        try
        {
            var items = JsonSerializer.Deserialize<UploadHeader>(header, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true});
            Guard.IsNotNull(items);
            var user = _identifier.ParseClaims(HttpContext.User);
            Guard.IsTrue(items.Items.Count == files.Count);
            var uls = new List<ImageUpload>(files.Count);
            for (int i = 0; i < items.Items.Count; i++)
            {
                var ul = new ImageUpload
                {
                    Prefix = items.Prefix,
                    Name = files[i].FileName,
                    Image = files[i].OpenReadStream(),
                    Visibility = items.Items[i].Visibility,
                    Owner = user
                };
                uls.Add(ul);
            }
            var resp = await _service.Upload(uls).ConfigureAwait(false);
            var r = new GetImagesResponse(resp);
            return new ActionResult<GetImagesResponse>(r);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in post files");
            return StatusCode(500);
        }
    }

    [HttpPost("download")]
    [Authorize(Policy = Policies.DownloadPolicy)]
    public async Task<ActionResult<NamedUri>> Download(DownloadImagesRequest images)
    {
        try
        {
            var user = _identifier.ParseClaims(HttpContext.User);
            _logger.LogInformation("Received download request from {Id}", user.Id);
            var archive = await _service.CreateArchive(user.Id, images.Images).ConfigureAwait(false);
            return archive;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating download");
            return StatusCode(500);
        }
    }
    
    #endregion

    #region Dirs
    
    [HttpGet("dirs")]
    [RedirectOnPolicyFail(Policies.AuthenticatedPolicy, "dir-samples")]
    public async Task<ActionResult<IReadOnlyList<DirectoryTreeItem>>> GetDirectories()
    {
        try
        {
            var user = _identifier.ParseClaims(HttpContext.User);
            var usr = await _users.GetUser(user.Id).ConfigureAwait(false);
            var dirs = await _service.GetDirectories(usr.ViewableIdentities()).ConfigureAwait(false);
            return new ActionResult<IReadOnlyList<DirectoryTreeItem>>(dirs);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting directories");
            return StatusCode(500);
        }
    }

    [HttpGet("dir-samples")]
    public Task<ActionResult<IReadOnlyList<DirectoryTreeItem>>> GetDirectorySamples()
    {
        var children = new List<DirectoryTreeItem>();
        var parent = new DirectoryTreeItem("Root", null, children) { Source = Guid.NewGuid() };
        parent.FileCount = 1;
        var child = new DirectoryTreeItem("Sample1", parent, new List<DirectoryTreeItem>());
        child.FileCount = 1;
        children.Add(child);
        return Task.FromResult(new ActionResult<IReadOnlyList<DirectoryTreeItem>>(
            new List<DirectoryTreeItem>
            {
                parent
            }));
    }

    #endregion
}
