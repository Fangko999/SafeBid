using MediatR;

namespace SafeBid.Application;

public record WithdrawCommand(Guid UserId, decimal Amount) : IRequest;
