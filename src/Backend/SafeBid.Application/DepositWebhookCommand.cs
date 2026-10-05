using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public record DepositWebhookCommand(
    string TransactionId,
    string ReferenceId,
    decimal Amount,
    string Status,
    long Timestamp) : IRequest<Result<Unit>>;
