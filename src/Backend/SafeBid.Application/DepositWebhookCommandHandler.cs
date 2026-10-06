using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public class DepositWebhookCommandHandler : IRequestHandler<DepositWebhookCommand, Result<Unit>>
{
    private readonly IAppDbContext _db;

    public DepositWebhookCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Unit>> Handle(DepositWebhookCommand request, CancellationToken cancellationToken)
    {
        if (request.Status != "SUCCESS")
        {
            // Ignore failed deposits, just return success so webhook provider doesn't retry
            return Result<Unit>.Success(Unit.Value);
        }

        if (request.Amount <= 0)
        {
            return Result<Unit>.Failure(new Error("Webhook.InvalidAmount", "Amount must be greater than zero"));
        }

        if (!Guid.TryParse(request.ReferenceId, out var userId))
        {
            return Result<Unit>.Failure(new Error("Webhook.InvalidReferenceId", "ReferenceId is not a valid GUID"));
        }

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet == null)
        {
            return Result<Unit>.Failure(new Error("Webhook.WalletNotFound", "Wallet not found for the given reference Id"));
        }

        var entry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            Status = "COMPLETED",
            CreatedAt = DateTime.UtcNow
        };

        if (wallet.IsConfiscated)
        {
            entry.WalletId = null;
            entry.Type = "DEPOSIT_CONFISCATION";
        }
        else
        {
            wallet.AvailableBalance += request.Amount;
            entry.WalletId = wallet.Id;
            entry.Type = "DEPOSIT";
        }

        _db.LedgerEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
