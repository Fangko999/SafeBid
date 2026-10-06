using MediatR;
using SafeBid.Domain;

namespace SafeBid.Application;

public record ToggleWatchlistCommand(Guid AuctionId, Guid UserId) : IRequest<Result<bool>>;

public class ToggleWatchlistCommandHandler : IRequestHandler<ToggleWatchlistCommand, Result<bool>>
{
    private readonly IAppDbContext _context;

    public ToggleWatchlistCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<bool>> Handle(ToggleWatchlistCommand request, CancellationToken cancellationToken)
    {
        var auction = await _context.Auctions.FindAsync(new object[] { request.AuctionId }, cancellationToken);
        if (auction == null)
            return Result<bool>.Failure(new Error("Auction.NotFound", "Auction not found"));

        var item = await _context.WatchlistItems.FindAsync(new object[] { request.UserId, request.AuctionId }, cancellationToken);

        bool isWatched;
        if (item == null)
        {
            _context.WatchlistItems.Add(new WatchlistItem
            {
                UserId = request.UserId,
                AuctionId = request.AuctionId,
                CreatedAt = DateTime.UtcNow
            });
            isWatched = true;
        }
        else
        {
            _context.WatchlistItems.Remove(item);
            isWatched = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(isWatched);
    }
}
