using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public record BidDto(Guid Id, decimal Amount, DateTime Timestamp, string MaskedBidderName);
public record GetBidHistoryQuery(Guid AuctionId) : IRequest<Result<List<BidDto>>>;

public class GetBidHistoryQueryHandler : IRequestHandler<GetBidHistoryQuery, Result<List<BidDto>>>
{
    private readonly IAppDbContext _context;

    public GetBidHistoryQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<BidDto>>> Handle(GetBidHistoryQuery request, CancellationToken cancellationToken)
    {
        var auctionExists = await _context.Auctions.AnyAsync(a => a.Id == request.AuctionId, cancellationToken);
        if (!auctionExists)
            return Result<List<BidDto>>.Failure(new Error("Auction.NotFound", "Auction not found"));

        // Load into memory first to apply custom string logic, or apply simpler SQL logic
        var bidsRaw = await _context.Bids
            .Include(b => b.Bidder)
            .Where(b => b.AuctionId == request.AuctionId)
            .OrderByDescending(b => b.Amount)
            .ToListAsync(cancellationToken);

        var bids = bidsRaw.Select(b => new BidDto(
            b.Id,
            b.Amount,
            b.Timestamp,
            MaskName(b.Bidder!.FullName)
        )).ToList();

        return Result<List<BidDto>>.Success(bids);
    }

    private static string MaskName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "Anonymous";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, 1) + "***";
        var firstName = parts.Last();
        var lastName = parts.First();
        return $"{lastName} {firstName.Substring(0, 1)}***";
    }
}
