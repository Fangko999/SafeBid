using MediatR;
using Microsoft.AspNetCore.Mvc;
using SafeBid.Application;
using System.Threading;
using System.Threading.Tasks;

namespace SafeBid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), ct);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }
        return BadRequest(new { Error = result.Error.Message });
    }
}
