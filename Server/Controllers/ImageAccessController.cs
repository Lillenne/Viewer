using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Viewer.Shared;

namespace Viewer.Server.Controllers;

[ApiController]
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

    [HttpPost("dirs")]
    public async Task<ActionResult<IReadOnlyList<DirectoryTreeItem>>> PostDirectories([FromBody] string dir)
    {
        var res = await _service.GetDirectories(dir).ConfigureAwait(false);
        return new ActionResult<IReadOnlyList<DirectoryTreeItem>>(res);
    }

    [HttpGet("dirs")]
    public async Task<ActionResult<IReadOnlyList<DirectoryTreeItem>>> GetDirectories()
    {
        var dirs = await _service.GetDirectories("").ConfigureAwait(false);
        return new ActionResult<IReadOnlyList<DirectoryTreeItem>>(dirs);
    }

    [HttpGet]
    public async Task<ActionResult<GetImagesResponse>> Get(GetImagesRequest request) // TODO pass as query string -- param does not work
    {
        var response = await _service.GetImages(request);
        return new ActionResult<GetImagesResponse>(response);
    }
}
