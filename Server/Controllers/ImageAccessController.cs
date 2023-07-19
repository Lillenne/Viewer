using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Viewer.Server.Services;
using Viewer.Shared;
using Viewer.Shared.Requests;

namespace Viewer.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ImageAccessController : ControllerBase
{
    private readonly IImageService _service;

    public ImageAccessController(IImageService service)
    {
        _service = service;
    }

    [HttpPost]
    public Task<ActionResult<GetImagesResponse>> Post(GetImagesRequest request)
    {
        return Get(request);
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
        var res = await _service.GetDirectories(dir ?? string.Empty).ConfigureAwait(false);
        return new ActionResult<IReadOnlyList<DirectoryTreeItem>>(res);
    }

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
        catch (Exception ex)
        {
            return BadRequest();
        }
    }
}
