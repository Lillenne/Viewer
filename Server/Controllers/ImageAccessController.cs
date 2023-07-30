using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viewer.Server.Services;
using Viewer.Shared;
using Viewer.Shared.Requests;
using Viewer.Shared.Services;

namespace Viewer.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ImageAccessController : ControllerBase
{
    private readonly ILogger<ImageAccessController> _logger;
    private readonly IImageService _service;

    public ImageAccessController(ILogger<ImageAccessController> logger, IImageService service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpPost]
    public Task<ActionResult<GetImagesResponse>> Post(GetImagesRequest request)
    {
        return Get(request);
    }
    
    [HttpPost("upload")]
    // TODO make own response with upload success, err, etc
    public async Task<ActionResult<GetImagesResponse>> PostFiles([FromForm] IEnumerable<IFormFile> files)
    {
        return BadRequest();
        try
        {
            // TODO ImageUpload Stream instead of byte[]
            var uploads = files.Select(f => new ImageUpload(f.FileName, f.OpenReadStream()));
            var resp = await _service.Upload(uploads).ConfigureAwait(false);
            var r = new GetImagesResponse(resp);
            return new ActionResult<GetImagesResponse>(r);
        }
        catch
        {
            return BadRequest();
        }
    }

    [HttpPost("image")]
    public async Task<ActionResult<ImageId>> Image(GetImageRequest request)
    {
        try
        {
            return new ActionResult<ImageId>(
                await _service.GetImage(request).ConfigureAwait(false)
            );
        }
        catch
        {
            return NotFound();
        }
    }

    [HttpPost("dirs")]
    public async Task<ActionResult<IReadOnlyList<DirectoryTreeItem>>> PostDirectories(
        [FromBody] string? dir
    )
    {
        try
        {
            var res = await _service.GetDirectories(dir ?? ROOT_DIR).ConfigureAwait(false);
            return new ActionResult<IReadOnlyList<DirectoryTreeItem>>(res);
        }
        catch (Exception ex)
        {
            return BadRequest();
        }
    }

    private const string ROOT_DIR = "/";

    [HttpGet("dirs")]
    public async Task<ActionResult<IReadOnlyList<DirectoryTreeItem>>> GetDirectories()
    {
        var dirs = await _service.GetDirectories(string.Empty).ConfigureAwait(false);
        return new ActionResult<IReadOnlyList<DirectoryTreeItem>>(dirs);
    }

    [HttpGet]
    public async Task<ActionResult<GetImagesResponse>> Get(GetImagesRequest request) // TODO pass as query string -- param does not work
    {
        try
        {
            var response = await _service.GetImages(request).ConfigureAwait(false);
            return new ActionResult<GetImagesResponse>(response);
        }
        catch
        {
            return BadRequest();
        }
    }
}
