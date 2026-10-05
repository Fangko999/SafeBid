using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public record LoginCommand(
    string Email,
    string Password) : IRequest<Result<string>>;
