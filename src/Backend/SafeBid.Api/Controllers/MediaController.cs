using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SafeBid.Application;

namespace SafeBid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload")]
    [Authorize]
    public async Task<IActionResult> UploadMedia(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Error = "File is required." });
        }

        using var stream = file.OpenReadStream();
        var command = new UploadMediaCommand(stream, file.FileName, file.ContentType, file.Length);
        
        var result = await _mediator.Send(command, ct);

        if (result.IsSuccess)
        {
            return Ok(new { url = result.Value });
        }

        return BadRequest(new { Error = result.Error.Message });
    }
}
