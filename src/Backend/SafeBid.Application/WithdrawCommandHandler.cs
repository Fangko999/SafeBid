using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand>
{
    private readonly IAppDbContext _dbContext;

    public WithdrawCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
        
        if (wallet == null)
            throw new InvalidOperationException("Wallet not found");

        if (wallet.AvailableBalance < request.Amount)
            throw new InvalidOperationException("Insufficient funds");

        wallet.AvailableBalance -= request.Amount;
        wallet.HoldAmount += request.Amount;

        _dbContext.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Amount = -request.Amount,
            Type = "WITHDRAWAL",
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Amount = request.Amount,
            Type = "WITHDRAWAL_HOLD",
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        });

        _dbContext.WithdrawalRequests.Add(new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Amount = request.Amount,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        });

        // This will automatically throw DbUpdateConcurrencyException if the RowVersion doesn't match
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
