using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public record GetWalletTransactionsQuery(Guid UserId, int Page, int PageSize) : IRequest<PaginatedResult<LedgerEntry>>;
