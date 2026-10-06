using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public class GetWalletTransactionsQueryHandler : IRequestHandler<GetWalletTransactionsQuery, PaginatedResult<LedgerEntry>>
{
    private readonly IAppDbContext _context;

    public GetWalletTransactionsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<LedgerEntry>> Handle(GetWalletTransactionsQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

        if (wallet == null)
        {
            return new PaginatedResult<LedgerEntry>(new List<LedgerEntry>(), 0, request.Page, request.PageSize);
        }

        var query = _context.LedgerEntries
            .AsNoTracking()
            .Where(l => l.WalletId == wallet.Id)
            .OrderByDescending(l => l.CreatedAt);

        var count = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<LedgerEntry>(items, count, request.Page, request.PageSize);
    }
}
