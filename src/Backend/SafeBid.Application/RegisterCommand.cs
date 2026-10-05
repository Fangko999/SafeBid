using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string PhoneNumber,
    string Cccd) : IRequest<Result<Guid>>;
