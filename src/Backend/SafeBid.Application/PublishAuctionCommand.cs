using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public record PublishAuctionCommand(Guid AuctionId, Guid UserId) : IRequest<Result<Unit>>;

public class PublishAuctionCommandHandler : IRequestHandler<PublishAuctionCommand, Result<Unit>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IStorageService _storageService;

    public PublishAuctionCommandHandler(IAppDbContext dbContext, IStorageService storageService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
    }

    public async Task<Result<Unit>> Handle(PublishAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _dbContext.Auctions
            .Include(a => a.Media)
            .FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken);

        if (auction == null)
            return Result<Unit>.Failure(new Error("Auction.NotFound", "Auction not found"));

        if (auction.SellerId != request.UserId)
            return Result<Unit>.Failure(new Error("Auction.Unauthorized", "You are not authorized to publish this auction"));

        var publishResult = auction.Publish();
        if (!publishResult.IsSuccess)
            return Result<Unit>.Failure(publishResult.Error);

        var fee = publishResult.Value;

        using var transaction = await ((DbContext)_dbContext).Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);
            if (wallet == null)
                return Result<Unit>.Failure(new Error("Wallet.NotFound", "Wallet not found"));

            if (wallet.AvailableBalance < fee)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<Unit>.Failure(new Error("Wallet.InsufficientFunds", "Insufficient funds to publish auction"));
            }

            wallet.AvailableBalance -= fee;

            _dbContext.LedgerEntries.Add(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = -fee,
                Type = "LISTING_FEE",
                Status = "COMPLETED",
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            var oldUrls = auction.Media.Select(m => m.MediaUrl).ToList();
            if (oldUrls.Any())
            {
                var newUrls = await _storageService.MoveFilesToPublicAsync(oldUrls, cancellationToken);
                
                var mediaList = auction.Media.ToList();
                for (int i = 0; i < mediaList.Count; i++)
                {
                    mediaList[i].UpdateUrl(newUrls[i]);
                }
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw; // Let global handler catch it as 500, or you can return Result.Failure
        }
    }
}
