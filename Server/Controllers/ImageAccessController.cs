using System.Text.Json;
using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viewer.Server.Services.AuthServices;
using Viewer.Server.Services.ImageServices;
using Viewer.Shared;

namespace Viewer.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ImageAccessController : ControllerBase
{
    private const string RootDir = "/";
    private readonly ILogger<ImageAccessController> _logger;
    private readonly IImageService _service;
    private readonly IClaimsParser _identifier;

    public ImageAccessController(ILogger<ImageAccessController> logger, IImageService service, IClaimsParser identifier)
    {
        _logger = logger;
        _service = service;
        _identifier = identifier;
    }

    #region Post
    
    [HttpPost]
    public async Task<ActionResult<GetImagesResponse>> Post(GetImagesRequest request)
    {
        try
        {
            var response = await _service.GetImageIds(request).ConfigureAwait(false);
            return new ActionResult<GetImagesResponse>(response);
        }
        catch
        {
            return BadRequest();
        }
    }
    
    [HttpPost("upload")]
    public async Task<ActionResult<GetImagesResponse>> PostFiles([FromForm] string header, [FromForm] IList<IFormFile> files)
    {
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
        catch
        {
            return BadRequest();
        }
    }

    [HttpPost("image")]
    public async Task<ActionResult<NamedUri>> Image(GetImageRequest request)
    {
        try
        {
            return new ActionResult<NamedUri>(await _service.GetImageId(request).ConfigureAwait(false));
        }
        catch
        {
            return NotFound();
        }
    }

    #endregion

    #region Get
    
    [HttpGet("dirs")]
    public async Task<ActionResult<IReadOnlyList<DirectoryTreeItem>>> GetDirectories()
    {
        var user = _identifier.ParseClaims(HttpContext.User);
        var dirs = await _service.GetDirectories(user).ConfigureAwait(false);
        return new ActionResult<IReadOnlyList<DirectoryTreeItem>>(dirs);
    }

    #endregion
}
