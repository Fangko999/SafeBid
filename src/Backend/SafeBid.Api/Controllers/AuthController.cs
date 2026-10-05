using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SafeBid.Application;

namespace SafeBid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public record RegisterRequest(string email, string password, string confirmPassword, string fullName, string phoneNumber);

    [HttpPost("register")]
    [EnableRateLimiting("RegisterLimit")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.email,
            request.password,
            request.confirmPassword,
            request.fullName,
            request.phoneNumber
        );

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error.Code == "User.DuplicateEmail" || result.Error.Code == "User.DuplicatePhone" || result.Error.Code == "User.PasswordMismatch")
            {
                return BadRequest(new { error = new { code = result.Error.Code, message = result.Error.Message } });
            }
            return BadRequest(result.Error);
        }

        return Created("", new { UserId = result.Value });
    }
}
