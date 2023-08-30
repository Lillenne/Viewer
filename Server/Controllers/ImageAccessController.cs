using System.Security.Claims;
using System.Text.Json;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viewer.Server.Services;
using Viewer.Server.Services.AuthServices;
using Viewer.Server.Services.ImageServices;
using Viewer.Shared;

namespace Viewer.Server.Controllers;

[ApiController]
[Authorize]
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

    #region Post
    
    [HttpPost]
    public async Task<ActionResult<GetImagesResponse>> Post(GetImagesRequest request)
    {
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
    
    [HttpPost("upload")]
    // TODO [Authorize(Policy = "uploader")]
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

    [HttpPost("image")]
    public async Task<ActionResult<NamedUri>> Image(GetImageRequest request)
    {
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

    #endregion

    #region Get
    
    [HttpGet("dirs")]
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

    #endregion
}
