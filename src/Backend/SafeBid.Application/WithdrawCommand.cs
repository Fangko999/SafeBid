using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public record WithdrawCommand(Guid UserId, decimal Amount) : IRequest<Result<Unit>>;
