using MediatR;
using Microsoft.EntityFrameworkCore;
using SafeBid.Domain;

namespace SafeBid.Application;

public record GetAuctionByIdQuery(Guid Id, Guid? UserId = null) : IRequest<Result<AuctionDetailDto>>;

public class AuctionDetailDto : AuctionDto
{
    public decimal StepPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public bool IsWatched { get; set; }
    public List<string> MediaUrls { get; set; } = new();
}

public class GetAuctionByIdQueryHandler : IRequestHandler<GetAuctionByIdQuery, Result<AuctionDetailDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetAuctionByIdQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AuctionDetailDto>> Handle(GetAuctionByIdQuery request, CancellationToken cancellationToken)
    {
        var auction = await (from a in _dbContext.Auctions
                             where a.Id == request.Id
                             join c in _dbContext.Categories on a.CategoryId equals c.Id
                             join u in _dbContext.Users on a.SellerId equals u.Id
                             select new
                             {
                                 Auction = a,
                                 CategoryName = c.Name,
                                 SellerName = u.FullName
                             }).FirstOrDefaultAsync(cancellationToken);

        if (auction == null)
        {
            return Result<AuctionDetailDto>.Failure(new Error("Auction.NotFound", "Auction not found"));
        }

        var mediaUrls = await _dbContext.AuctionMedia
            .Where(m => m.AuctionId == auction.Auction.Id)
            .OrderBy(m => m.SortOrder)
            .Select(m => m.MediaUrl)
            .ToListAsync(cancellationToken);

        bool isWatched = false;
        if (request.UserId.HasValue)
        {
            isWatched = await _dbContext.WatchlistItems.AnyAsync(w => w.AuctionId == request.Id && w.UserId == request.UserId.Value, cancellationToken);
        }

        var dto = new AuctionDetailDto
        {
            Id = auction.Auction.Id,
            Title = auction.Auction.Title,
            CurrentPrice = auction.Auction.CurrentPrice,
            StartPrice = auction.Auction.StartPrice,
            StepPrice = auction.Auction.StepPrice,
            ReservePrice = auction.Auction.ReservePrice,
            BuyNowPrice = auction.Auction.BuyNowPrice,
            EndTime = auction.Auction.EndTime,
            Status = auction.Auction.Status.ToString(),
            CategoryName = auction.CategoryName,
            SellerName = auction.SellerName,
            MainImageUrl = mediaUrls.FirstOrDefault() ?? string.Empty,
            MediaUrls = mediaUrls,
            IsWatched = isWatched
        };

        return Result<AuctionDetailDto>.Success(dto);
    }
}
