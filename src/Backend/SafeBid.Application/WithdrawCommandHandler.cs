using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public class WithdrawCommandHandler : IRequestHandler<WithdrawCommand, Result<Unit>>
{
    private readonly IAppDbContext _dbContext;

    public WithdrawCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Unit>> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return Result<Unit>.Failure(new Error("Wallet.InvalidAmount", "Amount must be greater than zero"));

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
        
        if (wallet == null)
            return Result<Unit>.Failure(new Error("Wallet.NotFound", "Wallet not found"));

        if (wallet.AvailableBalance < request.Amount)
            return Result<Unit>.Failure(new Error("Wallet.InsufficientFunds", "Insufficient funds"));

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

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<Unit>.Failure(new Error("Wallet.Concurrency", "Concurrency conflict. Please try again."));
        }
    }
}
