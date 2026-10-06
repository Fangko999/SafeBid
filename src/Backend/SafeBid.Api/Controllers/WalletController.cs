using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeBid.Application;

namespace SafeBid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IMediator _mediator;

    public WalletController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(idClaim!);
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = GetUserId();
        var wallet = await _mediator.Send(new GetBalanceQuery(userId));
        
        if (wallet == null) return NotFound();

        return Ok(new WalletBalanceDto(wallet.AvailableBalance, wallet.HoldAmount));
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;
        
        var userId = GetUserId();
        var result = await _mediator.Send(new GetWalletTransactionsQuery(userId, page, pageSize));
        
        return Ok(result);
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
    {
        var userId = GetUserId();
        var result = await _mediator.Send(new WithdrawCommand(userId, request.Amount));
        
        if (result.IsSuccess)
        {
            return Ok();
        }

        if (result.Error.Code == "Wallet.Concurrency")
        {
            return Conflict(new { message = result.Error.Message });
        }
        
        return BadRequest(new { message = result.Error.Message });
    }
}

public record WithdrawRequest(decimal Amount);
public record WalletBalanceDto(decimal AvailableBalance, decimal HoldAmount);
