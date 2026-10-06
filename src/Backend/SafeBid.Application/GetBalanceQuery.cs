using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public record GetBalanceQuery(Guid UserId) : IRequest<Wallet>;

public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, Wallet?>
{
    private readonly IAppDbContext _dbContext;

    public GetBalanceQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Wallet?> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
    }
}
