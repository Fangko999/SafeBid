using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeBid.Application;
using System.Threading;
using System.Threading.Tasks;

namespace SafeBid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuctionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuctionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateDraft([FromBody] CreateAuctionRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var sellerId))
            return Unauthorized();

        var command = new CreateAuctionCommand(sellerId, request.Title, request.CategoryId, request.StartPrice, request.StepPrice, request.ReservePrice, request.BuyNowPrice, request.StartTime, request.EndTime, request.MediaUrls);
        var result = await _mediator.Send(command, ct);
        if (result.IsSuccess)
        {
            return Created($"/api/auctions/{result.Value}", new { id = result.Value });
        }
        
        if (result.Error.Code == "Auction.Forbidden")
            return StatusCode(403, new { Error = result.Error.Message });

        return BadRequest(new { Error = result.Error.Message });
    }

    [HttpPost("{id}/publish")]
    [Authorize]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var sellerId))
            return Unauthorized();

        var command = new PublishAuctionCommand(id, sellerId);
        var result = await _mediator.Send(command, ct);
        
        if (result.IsSuccess)
            return Ok();

        if (result.Error.Code == "Auction.Unauthorized")
            return StatusCode(403, new { Error = result.Error.Message });
            
        return BadRequest(new { Error = result.Error.Message });
    }

    [HttpGet]
    public async Task<IActionResult> GetAuctions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var query = new GetAuctionsQuery(pageNumber, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAuctionById(Guid id)
    {
        Guid? userId = null;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var parsedId))
            userId = parsedId;

        var query = new GetAuctionByIdQuery(id, userId);
        var result = await _mediator.Send(query);
        if (!result.IsSuccess) return NotFound(new { Error = result.Error.Message });
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/watchlist")]
    [Authorize]
    public async Task<IActionResult> ToggleWatchlist(Guid id, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var command = new ToggleWatchlistCommand(id, userId);
        var result = await _mediator.Send(command, ct);
        if (result.IsSuccess) return Ok(new { isWatched = result.Value });
        return NotFound(new { Error = result.Error.Message });
    }

    [HttpGet("{id:guid}/bids")]
    public async Task<IActionResult> GetBidHistory(Guid id, CancellationToken ct)
    {
        var query = new GetBidHistoryQuery(id);
        var result = await _mediator.Send(query, ct);
        if (result.IsSuccess) return Ok(new { items = result.Value });
        return NotFound(new { Error = result.Error.Message });
    }
}

public record CreateAuctionRequest(
    string Title,
    Guid CategoryId,
    decimal StartPrice,
    decimal StepPrice,
    decimal? ReservePrice,
    decimal? BuyNowPrice,
    System.DateTime StartTime,
    System.DateTime EndTime,
    System.Collections.Generic.List<string> MediaUrls);
