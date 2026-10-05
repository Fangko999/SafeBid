using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string FullName,
    string PhoneNumber) : IRequest<Result<Guid>>;
